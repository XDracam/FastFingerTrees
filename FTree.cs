using System;
using System.Collections;
using System.Collections.Generic;

namespace FTrees
{
    using static FTree;
    
    /// <summary>
    /// A functional representation of persistent sequences supporting access to the ends in amortized constant time,
    ///  and concatenation and splitting in time logarithmic in the size of the smaller piece.
    /// </summary>
    /// <remarks> 
    /// Based on https://www.staff.city.ac.uk/~ross/papers/FingerTree.pdf
    /// </remarks>
    public abstract class FTree<T, V> : IEnumerable<T>, Measured<V>
    where T : Measured<V> where V : Measure<V>, new()
    {
        private FTree() { }

        public static FTree<T, V> Empty => EmptyT.Instance;
        
        public abstract V Measure { get; }

        internal sealed class EmptyT : FTree<T, V>
        {
            private EmptyT() { }
            public static readonly EmptyT Instance = new();
            public override V Measure => new();
        }
        
        internal sealed class Single : FTree<T, V>
        {
            public readonly T Value;
            public Single(T value) => Value = value;
            public void Deconstruct(out T value) { value = Value; }
            public override V Measure => Value.Measure;
        }

        internal sealed class Deep : FTree<T, V>
        {
            //public LazyThunk<V> Measure => new(measure);
            private readonly LazyThunk<V> measure;
            public readonly Digit<T> Left;
            public readonly LazyThunk<FTree<Node<T, V>, V>> Spine;
            public readonly Digit<T> Right;

            public Deep(
                Digit<T> left,
                LazyThunk<FTree<Node<T, V>, V>> spine,
                Digit<T> right
            ) {
                Left = left;
                Spine = spine;
                Right = right;
                measure = new(measureNow);
            }

            private V measureNow() => Left.measure<T, V>().Add(Spine.Value.Measure).Add(Right.measure<T, V>());
            
            public void Deconstruct(
                out Digit<T> left,
                out LazyThunk<FTree<Node<T, V>, V>> spine,
                out Digit<T> right
            ) {
                left = this.Left;
                spine = this.Spine;
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
            _ => throw new NotImplementedException()
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
            _ => throw new NotImplementedException()
        };
        
        public FTree<T, V> Prepend(T toAdd, bool isLazy = true) => this switch { // in paper: a <| this
            EmptyT => new Single(toAdd),
            Single(var x) => new Deep(new Digit<T>(toAdd), new(FTree<Node<T, V>, V>.EmptyT.Instance), new Digit<T>(x)),
            Deep({Values: {Length: 4} l}, var m, var sf) => 
                isLazy
                    ? new Deep(new Digit<T>(toAdd, l[0]), new(() => m.Value.Prepend(new Node<T, V>(l[1], l[2], l[3]), true)), sf)
                    : new Deep(new Digit<T>(toAdd, l[0]), new(m.Value.Prepend(new Node<T, V>(l[1], l[2], l[3]), false)), sf), 
            Deep(var pr, var m, var sf) => new Deep(pr.Prepend(toAdd), m, sf),
            _ => throw new NotImplementedException()
        };
        
        public FTree<T, V> Append(T toAdd, bool isLazy = true) => this switch { // in paper: this |> a
            EmptyT => new Single(toAdd),
            Single(var x) => new Deep(new Digit<T>(x), new(FTree<Node<T, V>, V>.EmptyT.Instance), new Digit<T>(toAdd)),
            Deep(var pr, var m, {Values: {Length: 4} r}) => 
                isLazy 
                    ? new Deep(pr, new(() => m.Value.Append(new Node<T, V>(r[0], r[1], r[2]), true)), new Digit<T>(r[3], toAdd))
                    : new Deep(pr, new(m.Value.Append(new Node<T, V>(r[0], r[1], r[2]), false)), new Digit<T>(r[3], toAdd)), 
            Deep(var pr, var m, var sf) => new Deep(pr, m, sf.Append(toAdd)),
            _ => throw new NotImplementedException()
        };

        public static FTree<T, V> Create(params T[] values) => CreateRange(values);
        
        public static FTree<T, V> CreateRange(IEnumerable<T> values, bool isLazy = false) {
            // TODO: this is slow
            FTree<T, V> res = EmptyT.Instance;
            foreach (var x in values) 
                res = res.Append(x, isLazy: false);
            return res;
        }

        public bool IsEmpty => this is EmptyT;
        
        public T Head => this switch {
            Single(var x) => x,
            Deep(var pr, _, _) => pr.Head,
            _ => throw new InvalidOperationException()
        };

        public FTree<T, V> Tail => toViewL(this).Tail.Value;
        
        public T Last => this switch {
            Single(var x) => x,
            Deep(_, _, var sf) => sf.Last,
            _ => throw new InvalidOperationException()
        };
        
        public FTree<T, V> Init => toViewR(this).Tail.Value;

        // in paper: |><| (wtf)
        public FTree<T, V> Concat(FTree<T, V> other) {
            return app3(this, new Digit<T>(Array.Empty<T>()), other);
            
            // PREPEND <|' -> reduceR
            // APPEND |>' -> reduceL

            static FTree<A, V> app3<A>(FTree<A, V> self, Digit<A> ts, FTree<A, V> other) where A : Measured<V> => 
                (self, other) switch {
                    // TODO: faster if switched on number of elements in digit
                    (FTree<A, V>.EmptyT, var xs) => ts.ReduceRight((c, acc) => acc.Prepend(c), xs),
                    (var xs, FTree<A, V>.EmptyT) => ts.ReduceLeft((acc, c) => acc.Append(c), xs),
                    (FTree<A, V>.Single(var x), var xs) => ts.ReduceRight((c, acc) => acc.Prepend(c), xs).Prepend(x),
                    (var xs, FTree<A, V>.Single(var x)) => ts.ReduceLeft((acc, c) => acc.Append(c), xs).Append(x),
                    (FTree<A, V>.Deep(var pr1, var m1, var sf1), FTree<A, V>.Deep(var pr2, var m2, var sf2)) => 
                        new FTree<A, V>.Deep(
                            pr1,
                            new(() => app3(
                                m1.Value,
                                new Digit<Node<A, V>>(nodes(concat(sf1.Values, ts.Values, pr2.Values))), 
                                m2.Value
                            )),
                            sf2
                        ),
                    _ => throw new NotImplementedException()
                };

            static A[] concat<A>(A[] first, A[] second, A[] third) {
                var res = new A[first.Length + second.Length + third.Length];
                Array.Copy(first, 0, res, 0, first.Length);
                Array.Copy(second, 0, res, first.Length, second.Length);
                Array.Copy(third, 0, res, first.Length + second.Length, third.Length);
                return res;
            }
            
            // optimized relative to the paper to avoid unnecessary allocations
            static Node<A, V>[] nodes<A>(A[] arr) where A : Measured<V> {
                var mod = arr.Length % 3;
                var res = new Node<A, V>[(arr.Length / 3) + (mod > 0 ? 1 : 0)];
                    
                // if mod = 2, then need a single Node2 => stop 2 before
                // if mod = 1, then need two Node2s => stop 4 before
                var regularEnd = arr.Length - (4 - mod);
                    
                var arrIdx = 0;
                var resIdx = 0;
                    
                while (arrIdx < regularEnd) {
                    res[resIdx] = new Node<A, V>(arr[arrIdx], arr[arrIdx + 1], arr[arrIdx + 2]);
                    resIdx += 1;
                    arrIdx += 3;
                }
                
                for (var i = 0; i < (3 - mod); ++i) {
                    res[resIdx] = new Node<A, V>(arr[arrIdx], arr[arrIdx + 1]);
                    resIdx += 1;
                    arrIdx += 2;
                }
                
                return res;
            }
        }
        
        // guaranteed to be O(logn)
        public (FTree<T, V>, T, FTree<T, V>) SplitTree(Func<V, bool> p, V i) {
            if (this is EmptyT) throw new InvalidOperationException();
            if (this is Single(var s)) return new(Empty, s, Empty);
            if (this is Deep(var pr, var m, var sf)) {
                var vpr = i.Add(pr.measure<T, V>());
                if (p(vpr)) {
                    var (l, x, r) = splitDigit(p, i, pr);
                    return new(CreateRange(l, isLazy: true), x, deepL(r, m, sf));
                }
                var vm = vpr.Add(m.Value.Measure);
                if (p(vm)) {
                    var (ml, xs, mr) = m.Value.SplitTree(p, vpr);
                    var (l, x, r) = splitDigit(p, vpr.Add(ml.Measure), xs.ToDigit());
                    return new(deepR<T, V>(pr, new(ml), l), x, deepL<T, V>(r, new(mr), sf));
                }
                else {
                    var (l, x, r) = splitDigit(p, vm, sf);
                    return new(deepR(pr, m, l), x, CreateRange(r, isLazy: true));
                }
            }
            throw new NotImplementedException();
            
            // digits have at most 4 items, so O(1)
            static (A[], A, A[]) splitDigit<A>(Func<V, bool> p, V i, Digit<A> digit) where A : Measured<V> {
                if (digit.Values.Length == 1)
                    return new(Array.Empty<A>(), digit.Head, Array.Empty<A>());
                for (var idx = 0; idx < digit.Values.Length; ++idx) {
                    i = i.Add(digit.Values[idx].Measure);
                    if (p(i)) return split(digit.Values, idx);
                }
                // not found anything, take last element
                return (digit.Init.Values, digit.Last, Array.Empty<A>());

                static (A[], A, A[]) split(A[] array, int idx) {
                    var firstSize = idx;
                    var secondSize = array.Length - idx - 1;
                    var first = new A[firstSize];
                    Array.Copy(array, 0, first, 0, firstSize);
                    var second = new A[secondSize];
                    Array.Copy(array, firstSize + 1, second, 0, secondSize);
                    return (first, array[idx], second);
                }
            }
        }
        
        // guaranteed to be O(logn)
        // like SplitTree, but doesn't generate unnecessary new trees
        // also should check for the first `i > target` and stop
        public ref readonly T LookupTree(V target, ref V i) {
            return ref lookupTree(target, ref i);
        }

        private ref readonly T lookupTree(V target, ref V i) {
            if (this is EmptyT) throw new InvalidOperationException();
            if (this is Single s) return ref s.Value;
            if (this is Deep(var pr, var m, var sf)) {
                var vpr = i.Add(pr.measure<T, V>());
                if (vpr.CompareTo(target) > 0) {
                    return ref lookupDigit(target, i, pr.Values);
                }

                i = vpr;
                var vm = vpr.Add(m.Value.Measure);
                if (vm.CompareTo(target) > 0) {
                    var xs = m.Value.lookupTree(target, ref vpr);
                    return ref lookupNode(target, vpr, xs); // vpr increased by tree lookup
                }

                i = vm;
                return ref lookupDigit(target, vm, sf.Values);
                
            }
            throw new NotImplementedException();
            
            static ref readonly T lookupNode(V target, V i, Node<T, V> node) {
                var i1 = i.Add(node.First.Measure);
                if (i1.CompareTo(target) > 0) 
                    return ref node.First;
                var i2 = i1.Add(node.Second.Measure);
                if (i2.CompareTo(target) > 0 || !node.HasThird) 
                    return ref node.Second;
                return ref node.Third;
            }
                
            static ref readonly T lookupDigit(V target, V i, T[] digit) {
                if (digit.Length == 1)
                    return ref digit[0];
                for (var idx = 0; idx < digit.Length; ++idx) {
                    var newI = i.Add(digit[idx].Measure);
                    if (newI.CompareTo(target) > 0) return ref digit[idx];
                    i = newI;
                }
                return ref digit[digit.Length - 1];
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

        // TODO: can we do a fast, allocation-free, lazy iterator?
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
                    if (node.HasThird) yield return node.Third;
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
        internal static View<A, V> toViewL<A, V>(FTree<A, V> self) where A : Measured<V> where V : Measure<V>, new() {
            return self switch {
                FTree<A, V>.EmptyT => new View<A, V>(),
                FTree<A, V>.Single(var x) => new View<A, V>(x, new(FTree<A, V>.EmptyT.Instance)),
                FTree<A, V>.Deep(var pr, var m, var sf) => new View<A, V>(pr.Head, new(() => deepL(pr.Tail.Values, m, sf))),
                _ => throw new NotImplementedException()
            };
        }
        
        internal static FTree<A, V> deepL<A, V>(
            A[] pr, 
            LazyThunk<FTree<Node<A, V>, V>> m, 
            Digit<A> sf
        ) where A : Measured<V> where V : Measure<V>, new() {
            if (pr.Length == 0) {
                var view = toViewL(m.Value);
                return view.IsCons 
                    ? new FTree<A, V>.Deep(view.Head.ToDigit(), view.Tail, sf) 
                    : FTree<A, V>.CreateRange(sf.Values, isLazy: true);
            }
            else return new FTree<A, V>.Deep(new(pr), m, sf);
        }
        
        
        internal static View<A, V> toViewR<A, V>(FTree<A, V> self) where A : Measured<V> where V : Measure<V>, new() {
            return self switch {
                FTree<A, V>.EmptyT => new View<A, V>(),
                FTree<A, V>.Single(var x) => new View<A, V>(x, new(FTree<A, V>.EmptyT.Instance)),
                FTree<A, V>.Deep(var pr, var m, var sf) => new View<A, V>(sf.Last, new(() => deepR(pr, m, sf.Init.Values))),
                _ => throw new NotImplementedException()
            };
        }
        
        internal static FTree<A, V> deepR<A, V>(
            Digit<A> pr,
            LazyThunk<FTree<Node<A, V>, V>> m, 
            A[] sf
        ) where A : Measured<V> where V : Measure<V>, new() {
            if (sf.Length == 0) {
                var view = toViewR(m.Value);
                return view.IsCons 
                    ? new FTree<A, V>.Deep(pr, view.Tail, view.Head.ToDigit()) 
                    : FTree<A, V>.CreateRange(pr.Values, isLazy: true);
            }
            else return new FTree<A, V>.Deep(pr, m, new(sf));
        }
    }

    internal readonly struct Digit<T>
    {
        public readonly T[] Values; // between 1 and 4 values
        public Digit(T[] values) => Values = values;

        public Digit(T a) => Values = new[]{a};
        public Digit(T a, T b) => Values = new[]{a, b};
        public Digit(T a, T b, T c) => Values = new[]{a, b, c}; 
        public Digit(T a, T b, T c, T d) => Values = new[]{a, b, c, d}; // "dangerous"

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

        public Digit<T> Prepend(T value) => Values.Length switch {
            1 => new(value, Values[0]),
            2 => new(value, Values[0], Values[1]),
            3 => new(value, Values[0], Values[1], Values[2]),
            _ => throw new InvalidOperationException()
        };
        
        public Digit<T> Append(T value) => Values.Length switch {
            1 => new(Values[0], value),
            2 => new(Values[0], Values[1], value),
            3 => new(Values[0], Values[1], Values[2], value),
            _ => throw new InvalidOperationException()
        };

        public T Head => Values[0];
        public Digit<T> Tail => Values.Length switch {
            1 => new(Array.Empty<T>()),
            2 => new(Values[1]),
            3 => new(Values[1], Values[2]),
            4 => new(Values[1], Values[2], Values[3]),
            _ => throw new InvalidOperationException()
        };
        
        public T Last => Values[Values.Length - 1];
        public Digit<T> Init => Values.Length switch {
            1 => new(Array.Empty<T>()),
            2 => new(Values[0]),
            3 => new(Values[0], Values[1]),
            4 => new(Values[0], Values[1], Values[2]),
            _ => throw new InvalidOperationException()
        };
    }

    public static class MeasureExtensions {
        internal static V measure<T, V>(this Digit<T> digit) where T : Measured<V> where V : Measure<V>, new() =>
            digit.Values.Length switch { // somehow faster than a loop
                1 => digit.Values[0].Measure,
                2 => digit.Values[0].Measure.Add(digit.Values[1].Measure),
                3 => digit.Values[0].Measure.Add(digit.Values[1].Measure).Add(digit.Values[2].Measure),
                4 => digit.Values[0].Measure.Add(digit.Values[1].Measure).Add(digit.Values[2].Measure).Add(digit.Values[3].Measure),
                _ => throw new NotImplementedException()
            };
    }

    public interface Measure<TSelf> : IComparable<TSelf> { TSelf Add(TSelf other); }
    public interface Measured<V> where V : Measure<V>, new() { V Measure { get; } }
    
    internal sealed class Node<T, V> : Measured<V> where T : Measured<V> where V : Measure<V>, new()
    {
        public readonly bool HasThird;
        public V Measure { get; }
        public readonly T First, Second, Third;

        public Node(T first, T second) {
            HasThird = false;
            First = first;
            Second = second;
            Third = default;
            Measure = first.Measure.Add(second.Measure);
        }
        
        public Node(T first, T second, T third) {
            HasThird = true;
            First = first;
            Second = second;
            Third = third;
            Measure = first.Measure.Add(second.Measure).Add(third.Measure);
        }

        public TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) {
            var start = HasThird ? reduceOp(Third, other) : other;
            return reduceOp(First, reduceOp(Second, start));
        }

        public TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) {
            var start = HasThird ? reduceOp(other, Third) : other;
            return reduceOp(reduceOp(start, Second), First);
        }

        public Digit<T> ToDigit() => HasThird ? new Digit<T>(First, Second, Third) : new Digit<T>(First, Second);
    }

    internal readonly struct View<T, V> where T : Measured<V> where V : Measure<V>, new()
    {
        public readonly bool IsCons;
        public readonly T Head;
        public readonly LazyThunk<FTree<T, V>> Tail;

        public View(T head, LazyThunk<FTree<T, V>> tail) {
            IsCons = true;
            Head = head;
            Tail = tail;
        }
    }
    
    // A Lazy<T> but less flexible but with less overhead
    internal readonly struct LazyThunk<T>
    {
        private readonly T _value;
        private readonly Lazy<T> _lazy;

        public T Value => _lazy == null ? _value : _lazy.Value;

        public LazyThunk(T value) {
            _value = value;
            _lazy = null;
        }

        public LazyThunk(Func<T> getter) {
            _value = default;
            _lazy = new(getter);
        }
    }
}