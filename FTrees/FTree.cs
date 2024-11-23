using System;
using System.Collections;
using System.Collections.Generic;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

using static FTreeImplUtils;

public interface IFTreeMeasure<TSelf> where TSelf : struct, IFTreeMeasure<TSelf>
{
    static abstract TSelf Add(params ReadOnlySpan<TSelf> values); // no overhead compared to Add(a, b) and overload
    static abstract TSelf Add<T>(ReadOnlySpan<T> values) where T : IFTreeElement<TSelf>; // for digits
}

public interface IFTreeElement<V> where V : struct, IFTreeMeasure<V> { V Measure { get; } }

/// <summary>
/// A functional representation of persistent sequences supporting access to the ends in amortized constant time,
///  and concatenation and splitting in time logarithmic in the size of the smaller piece.
/// </summary>
/// <remarks> 
/// Based on https://www.staff.city.ac.uk/~ross/papers/FingerTree.pdf
/// </remarks>
public abstract class FTree<T, V> : IEnumerable<T>, IFTreeElement<V>
where T : IFTreeElement<V> where V : struct, IFTreeMeasure<V>
{
    private FTree() { } // only allow nested types to inherit

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

    internal sealed class Deep(
        Digit<T, V> left,
        LazyThunk<FTree<Node<T, V>, V>> spine,
        Digit<T, V> right
    ) : FTree<T, V>
    {
        private V _measure;
        private bool _hasMeasure = false;
        
        public readonly Digit<T, V> Left = left;
        public readonly Digit<T, V> Right = right;
        public readonly LazyThunk<FTree<Node<T, V>, V>> Spine = spine;

        public void Deconstruct(
            out Digit<T, V> left,
            out LazyThunk<FTree<Node<T, V>, V>> outSpine,
            out Digit<T, V> right
        ) {
            left = Left;
            outSpine = Spine;
            right = Right;
        }

        public override V Measure => _hasMeasure ? _measure 
            : ((_measure, _hasMeasure) = (V.Add(Left.Measure, Spine.Value.Measure, Right.Measure), true))._measure;
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
    
    public ref readonly T Head {
        get {
            if (this is Single s) return ref s.Value;
            if (this is Deep d) return ref d.Left.Head;
            throw new InvalidOperationException();
        }
    }

    public FTree<T, V> Tail => toViewL(this).Tail;
    
    public ref readonly T Last {
        get {
            if (this is Single s) return ref s.Value;
            if (this is Deep d) return ref d.Right.Last;
            throw new InvalidOperationException();
        }
    }
    
    public FTree<T, V> Init => toViewR(this).Tail;

    // in paper: |><| (wtf)
    public FTree<T, V> Concat(FTree<T, V> other) => app3(this, new Digit<T, V>(), other);
    
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