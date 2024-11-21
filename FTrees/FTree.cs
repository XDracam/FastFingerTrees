using System;
using System.Collections;
using System.Collections.Generic;

namespace FTrees;

using static FTree;
using static Utils;

// Value tuples aren't ref structs, so this approach is faster
internal readonly ref struct Triple<T>
{
    public readonly ReadOnlySpan<T> Item1;
    public readonly T Item2;
    public readonly ReadOnlySpan<T> Item3;

    public Triple(ReadOnlySpan<T> item1, T item2, ReadOnlySpan<T> item3) {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
    }

    public void Deconstruct(out ReadOnlySpan<T> first, out T second, out ReadOnlySpan<T> third) {
        first = Item1;
        second = Item2;
        third = Item3;
    }
}

// Utils that do not depend on T and V in the FTree itself
internal static class Utils 
{
    // for Concat
    // PREPEND <|' -> reduceR
    // APPEND |>' -> reduceL
    internal static FTree<A, V> app3<A, V>(FTree<A, V> self, Digit<A, V> ts, FTree<A, V> other)
    where A : IMeasured<V> where V : struct, IMeasure<V> {
        return (self, other) switch {
            (FTree<A, V>.EmptyT, var xs) => prependDigit(ts, xs),
            (var xs, FTree<A, V>.EmptyT) => appendDigit(ts, xs),
            (FTree<A, V>.Single(var x), var xs) => prependDigit(ts, xs).Prepend(x),
            (var xs, FTree<A, V>.Single(var x)) => appendDigit(ts, xs).Append(x),
            (FTree<A, V>.Deep(var pr1, var m1, var sf1), FTree<A, V>.Deep(var pr2, var m2, var sf2)) => 
                new FTree<A, V>.Deep(
                    pr1,
                    new(() => app3(
                        m1.Value,
                        new Digit<Node<A, V>, V>(nodes<A, V>(concat(sf1.Values, ts.Values, pr2.Values))), 
                        m2.Value
                    )),
                    sf2
                ),
            _ => throw new InvalidOperationException()
        };
        
        static FTree<A, V> prependDigit(Digit<A, V> digit, FTree<A, V> tree) {
            for (var i = digit.Values.Length - 1; i >= 0; --i) 
                tree = tree.Prepend(digit.Values[i]);
            return tree;
        }
    
        static FTree<A, V> appendDigit(Digit<A, V> digit, FTree<A, V> tree) {
            for (var i = 0; i < digit.Values.Length; ++i) 
                tree = tree.Append(digit.Values[i]);
            return tree;
        }

        static A[] concat(A[] first, A[] second, A[] third) {
            // compute everything only once!
            var al = first.Length;
            var bl = second.Length;
            var cl = third.Length;
            var abl = al + bl;
            
            var res = new A[abl + cl];
            Array.Copy(first, 0, res, 0, al);
            Array.Copy(second, 0, res, al, bl);
            Array.Copy(third, 0, res, abl, cl);
            return res;
        }
    }
    
    // optimized to avoid unnecessary allocations
    internal static Node<A, V>[] nodes<A, V>(ReadOnlySpan<A> arr) 
    where A : IMeasured<V> where V : struct, IMeasure<V> {
        var length = arr.Length;
        var mod = length % 3;
        var res = new Node<A, V>[(length / 3) + (mod > 0 ? 1 : 0)];

        // 0 -> no node2s, 2 -> 1 node2, 1 -> 2 node2s (all nodes need 2 or 3 items)
        var numNode2 = (mod * 2) % 3; 
        var node3EndIdx = length - numNode2 * 2;
                
        var arrIdx = 0;
        var resIdx = 0;
                
        while (arrIdx < node3EndIdx)
            res[resIdx++] = new Node<A, V>(arr[arrIdx++], arr[arrIdx++], arr[arrIdx++]);
            
        for (var i = 0; i < numNode2; ++i)
            res[resIdx++] = new Node<A, V>(arr[arrIdx++], arr[arrIdx++]);
            
        return res;
    }
    
    // digits have at most 4 items, so O(1)
    internal static Triple<A> splitDigit<A, V>(Func<V, bool> p, V i, Digit<A, V> digit) 
    where A : IMeasured<V> where V : struct, IMeasure<V> {
        if (digit.Values.Length == 1)
            return new([], digit.Head, []);
        for (var idx = 0; idx < digit.Values.Length; ++idx) {
            i = V.Add(i, digit.Values[idx].Measure);
            if (p(i)) return split(digit.Values, idx);
        }
        // not found anything, take last element
        return new(digit.Init, digit.Last, []);

        static Triple<A> split(ReadOnlySpan<A> array, int idx) {
            var first = array[..idx];
            var second = array[(idx+1)..];
            return new(first, array[idx], second);
        }
    }
}

/// <summary>
/// A functional representation of persistent sequences supporting access to the ends in amortized constant time,
///  and concatenation and splitting in time logarithmic in the size of the smaller piece.
/// </summary>
/// <remarks> 
/// Based on https://www.staff.city.ac.uk/~ross/papers/FingerTree.pdf
/// </remarks>
public abstract class FTree<T, V> : IEnumerable<T>, IMeasured<V>
where T : IMeasured<V> where V : struct, IMeasure<V>
{
    private FTree() { }

    public static FTree<T, V> Empty => EmptyT.Instance;
    
    public abstract V Measure { get; }

    internal sealed class EmptyT : FTree<T, V>
    {
        private EmptyT() { }
        public static readonly EmptyT Instance = new();
        public override V Measure => default;
    }
    
    internal sealed class Single(T value) : FTree<T, V>
    {
        public readonly T Value = value;
        public void Deconstruct(out T value) { value = Value; }
        public override V Measure => Value.Measure;
    }

    internal sealed class Deep : FTree<T, V>
    {
        private LazyThunkClass<V> measure; // must be mutable for proper caching
        public readonly Digit<T, V> Left;
        private LazyThunkClass<FTree<Node<T, V>, V>> spine;  // must be mutable for proper caching
        public readonly Digit<T, V> Right;

        public FTree<Node<T, V>, V> Spine => spine.Value;

        public Deep(
            Digit<T, V> left,
            LazyThunkClass<FTree<Node<T, V>, V>> spine,
            Digit<T, V> right
        ) {
            Left = left;
            this.spine = spine;
            Right = right;
            measure = new(measureNow);
        }

        private V measureNow() => V.Add(Left.Measure, Spine.Measure, Right.Measure);
        
        public void Deconstruct(
            out Digit<T, V> left,
            out LazyThunkClass<FTree<Node<T, V>, V>> outSpine,
            out Digit<T, V> right
        ) {
            left = this.Left;
            outSpine = this.spine;
            right = this.Right;
        }

        public override V Measure => measure.Value;
    }
    
    public TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) => this switch {
        EmptyT => other,
        Single(var x) => reduceOp(x, other),
        Deep(var pr, var m, var sf) => 
            pr.ReduceRight(
                reduceOp, 
                m.Value.ReduceRight(
                    (a, b) => a.ReduceRight(reduceOp, b), 
                    sf.ReduceRight(reduceOp, other)
                )
            ),
        _ => throw new InvalidOperationException()
    };
    
    public TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) => this switch {
        EmptyT => other,
        Single(var x) => reduceOp(other, x),
        Deep(var pr, var m, var sf) => 
            sf.ReduceLeft(
                reduceOp, 
                m.Value.ReduceLeft(
                    (a, b) => b.ReduceLeft(reduceOp, a), 
                    pr.ReduceLeft(reduceOp, other)
                )
            ),
        _ => throw new InvalidOperationException()
    };
    
    public FTree<T, V> Prepend(T toAdd) => this switch { // in paper: a <| this
        EmptyT => new Single(toAdd),
        Single(var x) => new Deep(new Digit<T, V>(toAdd), new(FTree<Node<T, V>, V>.EmptyT.Instance), new Digit<T, V>(x)),
        Deep({Values: {Length: 4} l}, var m, var sf) => 
            new Deep(new Digit<T, V>(toAdd, l[0]), new(() => m.Value.Prepend(new Node<T, V>(l[1], l[2], l[3]))), sf),
        Deep(var pr, var m, var sf) => new Deep(pr.Prepend(toAdd), m, sf),
        _ => throw new InvalidOperationException()
    };
    
    public FTree<T, V> Append(T toAdd) => this switch { // in paper: this |> a
        EmptyT => new Single(toAdd),
        Single(var x) => new Deep(new Digit<T, V>(x), new(FTree<Node<T, V>, V>.EmptyT.Instance), new Digit<T, V>(toAdd)),
        Deep(var pr, var m, {Values: {Length: 4} r}) => 
            new Deep(pr, new(() => m.Value.Append(new Node<T, V>(r[0], r[1], r[2]))), new Digit<T, V>(r[3], toAdd)),
        Deep(var pr, var m, var sf) => new Deep(pr, m, sf.Append(toAdd)),
        _ => throw new InvalidOperationException()
    };

    public static FTree<T, V> Create(params ReadOnlySpan<T> values) => 
        CreateRange(values);
    
    public static FTree<T, V> CreateRange(ReadOnlySpan<T> values) =>
        createRangeOptimized(values);
    
    internal static FTree<T, V> createRangeOptimized(ReadOnlySpan<T> array) {
        var length = array.Length;
        switch (length) {
            case 0:
                return EmptyT.Instance;
            case 1:
                return new Single(array[0]);
            case <= 8:
                // Create a digit directly if possible
                var firstDigitLength = length / 2;
                return new Deep(
                    new Digit<T, V>(array[..firstDigitLength]), 
                    new(FTree<Node<T, V>, V>.EmptyT.Instance), 
                    new Digit<T, V>(array[firstDigitLength..])
                );
            default:
                var leftDigit = new Digit<T, V>(array[..3]);
                var rightDigit = new Digit<T, V>(array[^3..]);

                var arrForNodes = array[3..^3].ToArray();
                // Note: node needs 2 or 3 elements
                return new Deep(
                    leftDigit, 
                    new(() => FTree<Node<T, V>, V>.createRangeOptimized(nodes<T, V>(arrForNodes))),
                    rightDigit
                );
        }
    }

    public bool IsEmpty => this is EmptyT;
    
    public T Head => this switch {
        Single(var x) => x,
        Deep(var pr, _, _) => pr.Head,
        _ => throw new InvalidOperationException()
    };

    public FTree<T, V> Tail => toViewL(this).Tail;
    
    public T Last => this switch {
        Single(var x) => x,
        Deep(_, _, var sf) => sf.Last,
        _ => throw new InvalidOperationException()
    };
    
    public FTree<T, V> Init => toViewR(this).Tail;

    // in paper: |><| (wtf)
    public FTree<T, V> Concat(FTree<T, V> other) => app3(this, new Digit<T, V>([]), other);
    
    // guaranteed to be O(logn)
    public (FTree<T, V>, T, FTree<T, V>) SplitTree(Func<V, bool> p, V i) {
        switch (this) {
            case Single(var s): 
                return (Empty, s, Empty);
            case Deep(var pr, var m, var sf): 
                var vpr = V.Add(i, pr.Measure);
                if (p(vpr)) {
                    var (l, x, r) = splitDigit(p, i, pr);
                    return (createRangeOptimized(l), x, deepL(r, m, sf));
                }
                var vm = V.Add(vpr, m.Value.Measure);
                if (p(vm)) {
                    var (ml, xs, mr) = m.Value.SplitTree(p, vpr);
                    var (l, x, r) = splitDigit(p, V.Add(vpr, ml.Measure), xs.ToDigit());
                    return (deepR(pr, new(ml), l), x, deepL(r, new(mr), sf));
                }
                else {
                    var (l, x, r) = splitDigit(p, vm, sf);
                    return (deepR(pr, m, l), x, createRangeOptimized(r));
                }
            default: throw new InvalidOperationException("Cannot split an empty FTree");
        }
    }

    public (FTree<T, V>, FTree<T, V>) Split(Func<V, bool> p) {
        if (this is EmptyT) return (Empty, Empty);
        if (p(Measure)) {
            var (l, x, r) = SplitTree(p, new());
            return (l, r.Prepend(x));
        }
        return (this, Empty);
    }

    public FTree<T, V> TakeUntil(Func<V, bool> p) => Split(p).Item1;
    public FTree<T, V> DropUntil(Func<V, bool> p) => Split(p).Item2;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public IEnumerator<T> GetEnumerator() {
        if (this is EmptyT) yield break;
        else if (this is Single(var x)) yield return x;
        else if (this is Deep(var pr, var m, var sf)) {
            foreach (var elem in pr.Values) yield return elem;
            foreach (var node in m.Value) {
                yield return node.First;
                yield return node.Second;
                if (node.HasThird) yield return node.Third;
            }
            foreach (var elem in sf.Values) yield return elem;
        }
    }
    
    public IEnumerator<T> GetReverseEnumerator() {
        if (this is EmptyT) yield break;
        else if (this is Single(var x)) yield return x;
        else if (this is Deep(var pr, var m, var sf)) {
            for (var i = sf.Values.Length - 1; i >= 0; --i) 
                yield return sf.Values[i];
            var it = m.Value.GetReverseEnumerator();
            while (it.MoveNext()) {
                var node = it.Current;
                if (node!.HasThird) yield return node.Third;
                yield return node.Second;
                yield return node.First;
            }
            for (var i = pr.Values.Length - 1; i >= 0; --i) 
                yield return pr.Values[i];
        }
    }
}

public static class FTree
{
    internal static View<A, V> toViewL<A, V>(FTree<A, V> self) where A : IMeasured<V> where V : struct, IMeasure<V> {
        return self switch {
            FTree<A, V>.EmptyT => new View<A, V>(),
            FTree<A, V>.Single(var x) => new View<A, V>(x, new(FTree<A, V>.EmptyT.Instance)),
            FTree<A, V>.Deep(var pr, var m, var sf) => new View<A, V>(pr.Head, new(() => deepL(pr.Tail, m, sf))),
            _ => throw new InvalidOperationException()
        };
    }
    
    internal static FTree<A, V> deepL<A, V>(
        ReadOnlySpan<A> pr, 
        LazyThunkClass<FTree<Node<A, V>, V>> m, 
        Digit<A, V> sf
    ) where A : IMeasured<V> where V : struct, IMeasure<V> {
        if (pr.Length > 0) 
            return new FTree<A, V>.Deep(new(pr), m, sf);
        
        return m.Value switch {
            FTree<Node<A, V>, V>.EmptyT => 
                FTree<A, V>.createRangeOptimized(sf.Values),
            FTree<Node<A, V>, V>.Single(var x) => 
                new FTree<A, V>.Deep(x.ToDigit(), new(FTree<Node<A, V>, V>.Empty), sf),
            FTree<Node<A, V>, V>.Deep(var pr2, var m2, var sf2) => 
                new FTree<A, V>.Deep(pr2.Head.ToDigit(), new(() => deepL(pr2.Tail, m2, sf2)), sf),
            _ => throw new InvalidOperationException()
        };
    }
    
    
    internal static View<A, V> toViewR<A, V>(FTree<A, V> self) where A : IMeasured<V> where V : struct, IMeasure<V> {
        return self switch {
            FTree<A, V>.EmptyT => new View<A, V>(),
            FTree<A, V>.Single(var x) => new View<A, V>(x, new(FTree<A, V>.EmptyT.Instance)),
            FTree<A, V>.Deep(var pr, var m, var sf) => new View<A, V>(sf.Last, new(() => deepR(pr, m, sf.Init))),
            _ => throw new InvalidOperationException()
        };
    }
    
    internal static FTree<A, V> deepR<A, V>(
        Digit<A, V> pr,
        LazyThunkClass<FTree<Node<A, V>, V>> m, 
        ReadOnlySpan<A> sf
    ) where A : IMeasured<V> where V : struct, IMeasure<V> {
        if (sf.Length > 0) 
            return new FTree<A, V>.Deep(pr, m, new(sf));
        
        return m.Value switch {
            FTree<Node<A, V>, V>.EmptyT => 
                FTree<A, V>.createRangeOptimized(pr.Values),
            FTree<Node<A, V>, V>.Single(var x) => 
                new FTree<A, V>.Deep(pr, new(FTree<Node<A, V>, V>.Empty), x.ToDigit()),
            FTree<Node<A, V>, V>.Deep(var pr2, var m2, var sf2) => 
                new FTree<A, V>.Deep(pr, new(() => deepR(pr2, m2, sf2.Init)), sf2.Last.ToDigit()),
            _ => throw new InvalidOperationException()
        };
    }
}

internal readonly struct Digit<T, V> where T : IMeasured<V> where V : struct, IMeasure<V>
{
    public readonly T[] Values; // between 1 and 4 values
    private readonly V measure; // could be lazy, but that adds overhead on average

    public V Measure => measure;

    public Digit(params ReadOnlySpan<T> values) {
        Values = values.ToArray();
        measure = V.Add<T>(Values);
    }

    public TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) {
        var acc = other;
        for (var i = Values.Length - 1; i >= 0; --i) 
            acc = reduceOp(Values[i], acc);
        return acc;
    }

    public TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) {
        var acc = other;
        for (var i = 0; i < Values.Length; ++i) 
            acc = reduceOp(acc, Values[i]);
        return acc;
    }

    public Digit<T, V> Prepend(T value) => Values.Length switch {
        1 => new(value, Values[0]),
        2 => new(value, Values[0], Values[1]),
        3 => new(value, Values[0], Values[1], Values[2]),
        _ => throw new InvalidOperationException()
    };
    
    public Digit<T, V> Append(T value) => Values.Length switch {
        1 => new(Values[0], value),
        2 => new(Values[0], Values[1], value),
        3 => new(Values[0], Values[1], Values[2], value),
        _ => throw new InvalidOperationException()
    };

    public T Head => Values[0];
    public ReadOnlySpan<T> Tail => Values.AsSpan()[1..];
    
    public T Last => Values[^1];
    public ReadOnlySpan<T> Init => Values.AsSpan()[..^1];
}

public interface IMeasure<TSelf> where TSelf : struct, IMeasure<TSelf>
{
    static abstract TSelf Add(params ReadOnlySpan<TSelf> values);
    static abstract TSelf Add<T>(ReadOnlySpan<T> values) where T : IMeasured<TSelf>;
}

public interface IMeasured<V> where V : struct, IMeasure<V> { V Measure { get; } }

internal sealed class Node<T, V> : IMeasured<V> where T : IMeasured<V> where V : struct, IMeasure<V>
{
    public readonly bool HasThird;
    public readonly T First, Second, Third;

    private bool _hasMeasure;
    private V _measure;
    
    public V Measure {
        get {
            if (_hasMeasure) return _measure;
            _measure = HasThird
                ? V.Add(First.Measure, Second.Measure, Third.Measure)
                : V.Add(First.Measure, Second.Measure);
            _hasMeasure = true;
            return _measure;
        }
    }

    public Node(T first, T second) {
        HasThird = false;
        First = first;
        Second = second;
        Third = default;
    }
    
    public Node(T first, T second, T third) {
        HasThird = true;
        First = first;
        Second = second;
        Third = third;
    }

    public TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) {
        var start = HasThird ? reduceOp(Third, other) : other;
        return reduceOp(First, reduceOp(Second, start));
    }

    public TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) {
        var start = HasThird ? reduceOp(other, Third) : other;
        return reduceOp(reduceOp(start, Second), First);
    }

    public Digit<T, V> ToDigit() => HasThird ? new Digit<T, V>(First, Second, Third) : new Digit<T, V>(First, Second);
}

internal readonly struct View<T, V>(T head, LazyThunk<FTree<T, V>> tail)
    where T : IMeasured<V>
    where V : struct, IMeasure<V>
{
    public View() : this(default, default) { }
    public readonly bool IsCons = true;
    public readonly T Head = head;
    public FTree<T, V> Tail => tail.Value;
}

// Either a producer or an already calculated value.
// The extra level of indirection adds performance when initialized eagerly.
// A struct is worse in some cases, e.g. when reusing the thunk in case of Deep.
internal sealed class LazyThunkClass<T>
{
    private bool _hasValue;
    private T _value;
    private Func<T> _producer;

    public T Value { get {
        if (!_hasValue) {
            _value = _producer();
            _hasValue = true;
            _producer = null;
        }
        return _value;
    }}

    public LazyThunkClass(T value) {
        _value = value;
        _hasValue = true;
    }

    public LazyThunkClass(Func<T> getter) {
        _producer = getter;
    }
}

// Either a producer or an already calculated value.
// The extra level of indirection adds performance when initialized eagerly.
// Use carefully! You don't want to copy this by accident, making the cached value useless.
internal struct LazyThunk<T>
{
    private bool _hasValue;
    private T _value;
    private Func<T> _producer;

    public T Value { get {
        if (!_hasValue) {
            _value = _producer();
            _hasValue = true;
            _producer = null;
        }
        return _value;
    }}

    public LazyThunk(T value) {
        _value = value;
        _hasValue = true;
    }

    public LazyThunk(Func<T> getter) {
        _producer = getter;
    }
}