using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace FTrees
{
    public static class ImmutableOrderedSet
    {
        public static ImmutableOrderedSet<T> Create<T>(params ReadOnlySpan<T> values) where T : IComparable<T> {
            var res = ImmutableOrderedSet<T>.Empty;
            foreach (var item in values) res = res.Add(item);
            return res;
        }
        
        public static ImmutableOrderedSet<T> CreateRange<T>(IEnumerable<T> values) where T : IComparable<T> {
            var res = ImmutableOrderedSet<T>.Empty;
            foreach (var item in values) res = res.Add(item);
            return res;
        }
    }
    
    /// <summary>
    /// An immutable ordered sequence backed by a (2,3)-Finger-Tree.
    /// Keeps all elements sorted at all times. Can be iterated efficiently.
    /// All operations run in logarithmic time.
    /// Also allows efficient merging and partitioning in logarithmic time, unlike <see cref="ImmutableSortedSet{T}"/>.
    /// </summary>
    /// <remarks>
    /// An object may be present multiple times. Calling <see cref="Remove"/> will remove all instances.
    /// </remarks>
    [CollectionBuilder(typeof(ImmutableOrderedSet), nameof(ImmutableOrderedSet.Create))]
    public readonly struct ImmutableOrderedSet<T> : IEnumerable<T> where T : IComparable<T>
    {
        // Note: no support for IComparer: how would we even cache those? in all keys? therefore all elements?
        
        private readonly FTree<OrderedElem<T>, Key<T>> backing;
        internal ImmutableOrderedSet(FTree<OrderedElem<T>, Key<T>> backing) => this.backing = backing;
        
        public static ImmutableOrderedSet<T> Empty => new(FTree<OrderedElem<T>, Key<T>>.Empty);

        /// O(log n)
        public (ImmutableOrderedSet<T> Less, ImmutableOrderedSet<T> Greater) Partition(T k) {
            var (l, r) = backing.Split(x => x >= new Key<T>(k));
            return (new(l), new(r));
        }

        /// O(log n)
        public ImmutableOrderedSet<T> Add(T value) {
            if (backing.IsEmpty) 
                return new(new FTree<OrderedElem<T>, Key<T>>.Single(new(value)));
            var (l, x, r) = backing.SplitTree(x => x >= new Key<T>(value), new());
            return x.Measure.Value.CompareTo(value) switch {
                < 0 => new(l.Append(x).Append(new(value)).Concat(r)),
                0 => this, // already present
                > 0 => new(l.Append(new(value)).Append(x).Concat(r))
            };
        }

        /// O(log n)
        public ImmutableOrderedSet<T> Remove(T value) {
            var (l, r) = backing.Split(x => x >= new Key<T>(value));
            var (_, r2) = r.Split(x => x > new Key<T>(value));
            return new(l.Concat(r2));
        }

        // TODO: can this be optimized?
        /// Amortized theta(m log (n/m)) time (asymptotically optimal)
        public ImmutableOrderedSet<T> MergeWith(ImmutableOrderedSet<T> other) {
            return new(merge(backing, other.backing));
            
            static FTree<OrderedElem<T>, Key<T>> merge(
                FTree<OrderedElem<T>, Key<T>> xs, 
                FTree<OrderedElem<T>, Key<T>> ys
            ) {
                if (xs.IsEmpty) return ys;
                if (ys.IsEmpty) return xs;
                
                var view = FTree.toViewL(ys);
                
                // ReSharper disable once AccessToModifiedClosure // false positive
                var (l, x, r) = xs.SplitTree(x => x >= view.Head.Measure, new());
                if (x.Measure.CompareTo(view.Head.Measure) == 0)
                    return l.Concat(merge(view.Tail, r).Prepend(x));
                return l.Concat(merge(view.Tail, r.Prepend(x)).Prepend(view.Head));
            }
        }

        // TODO: benchmark this
        public bool Contains(T value) {
            var newKey = new Key<T>(value);
            var i = new Key<T>();
            var found = backing.LookupTree(ref newKey, ref i);
            return found.Value.CompareTo(value) == 0;
        }
        
#region Enumerator
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public Enumerator GetEnumerator() => new(backing.GetEnumerator());

        public readonly struct Enumerator : IEnumerator<T>
        {
            private readonly IEnumerator<OrderedElem<T>> backing;
            internal Enumerator(IEnumerator<OrderedElem<T>> backing) => this.backing = backing;

            public T Current => backing.Current.Value;
            object IEnumerator.Current => Current;
            
            public bool MoveNext() => backing.MoveNext();
            public void Reset() => backing.Reset();

            public void Dispose() => backing.Dispose();
        }
        
#endregion
    }

    internal static class ImmutableOrderedSetUtils {
        // guaranteed to be O(logn)
        // like SplitTree, but doesn't generate unnecessary new trees
        // also should check for the first `i > target` and stop
        internal static ref readonly T LookupTree<T, TKey>(
            this FTree<T, TKey> tree, 
            ref TKey target, 
            ref TKey i
        ) where T : IMeasured<TKey> where TKey : struct, IMeasure<TKey>, IComparable<TKey> {
            if (tree is FTree<T, TKey>.Single s) return ref s.Value;
            if (tree is FTree<T, TKey>.Deep(var pr, var m, var sf)) {
                var vpr = TKey.Add(i, pr.Measure);
                if (vpr.CompareTo(target) <= 0) {
                    return ref lookupDigit(ref target, ref i, pr.Values);
                }

                i = vpr;
                var mValue = m.Value;
                var vm = TKey.Add(vpr, mValue.Measure);
                if (vm.CompareTo(target) <= 0) {
                    var xs = LookupTree(mValue, ref target, ref i);
                    return ref lookupNode(ref target, ref i, xs);
                }

                i = vm;
                return ref lookupDigit(ref target, ref i, sf.Values);
            }
            throw new InvalidOperationException();
            
            static ref readonly T lookupNode(ref TKey target, ref TKey i, Node<T, TKey> node) {
                ref readonly var fst = ref node.First;
                var i1 = TKey.Add(i, fst.Measure);
                if (i1.CompareTo(target) <= 0) 
                    return ref fst;
                ref readonly var snd = ref node.Second;
                if (!node.HasThird || TKey.Add(i1, snd.Measure).CompareTo(target) <= 0) 
                    return ref snd;
                return ref node.Third;
            }
            
            static ref readonly T lookupDigit(ref TKey target, ref TKey i, T[] digit) {
                if (digit.Length == 1)
                    return ref digit[0];
                for (var idx = 0; idx < digit.Length; ++idx) {
                    ref var curr = ref digit[idx];
                    var newI = TKey.Add(i, curr.Measure);
                    if (newI.CompareTo(target) > 0) return ref curr;
                    i = newI;
                }
                return ref digit[digit.Length - 1];
            }
        }
    }

    internal readonly struct Key<T>(T value) : IMeasure<Key<T>>, IComparable<Key<T>>
        where T : IComparable<T>
    {
        public readonly bool HasValue = true;
        public readonly T Value = value;

        public Key<T> Add(in Key<T> other) => other.HasValue ? other : this; // always the highest key

        public int CompareTo(Key<T> other) {
            if (!HasValue) return -1;
            if (!other.HasValue) return 1;
            return Value.CompareTo(other.Value);
        }

        public static bool operator<(Key<T> left, Key<T> right) => left.CompareTo(right) < 0;
        public static bool operator>(Key<T> left, Key<T> right) => left.CompareTo(right) > 0;

        public static bool operator<=(Key<T> left, Key<T> right) => left.CompareTo(right) <= 0;
        public static bool operator>=(Key<T> left, Key<T> right) => left.CompareTo(right) >= 0;
        
        public static Key<T> Add(params ReadOnlySpan<Key<T>> values) {
            for (var i = values.Length - 1; i >= 0; --i) {
                ref readonly var curr = ref values[i];
                if (curr.HasValue) return curr;
            }
            return default;
        }

        public static Key<T> Add<A>(ReadOnlySpan<A> values) where A : IMeasured<Key<T>> {
            for (var i = values.Length - 1; i >= 0; --i) {
                var curr = values[i].Measure;
                if (curr.HasValue) return curr;
            }
            return default;
        }
    }

    internal readonly struct OrderedElem<T> : IMeasured<Key<T>> where T : IComparable<T>
    {
        public readonly T Value;
        public OrderedElem(T value) => Value = value;
        public Key<T> Measure => new(Value);
    }
}