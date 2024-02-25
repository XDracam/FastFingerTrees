using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace FTrees
{
    public static class ImmutableOrderedSeq
    {
        public static ImmutableOrderedSeq<T> Create<T>(params T[] values) where T : IComparable<T> => CreateRange(values);

        public static ImmutableOrderedSeq<T> CreateRange<T>(IEnumerable<T> values) where T : IComparable<T> {
            var res = ImmutableOrderedSeq<T>.Empty;
            foreach (var item in values) res = res.Insert(item);
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
    public readonly struct ImmutableOrderedSeq<T> : IEnumerable<T> where T : IComparable<T>
    {
        // Note: no support for IComparer: how would we even cache those? in all keys? therefore all elements?
        
        private readonly FTree<OrderedElem<T>, Key<T>> backing;
        internal ImmutableOrderedSeq(FTree<OrderedElem<T>, Key<T>> backing) => this.backing = backing;
        
        public static ImmutableOrderedSeq<T> Empty => new(FTree<OrderedElem<T>, Key<T>>.Empty);

        /// O(log n)
        public (ImmutableOrderedSeq<T> Less, ImmutableOrderedSeq<T> Greater) Partition(T k) {
            var (l, r) = backing.Split(x => x >= new Key<T>(k));
            return (new(l), new(r));
        }

        /// O(log n)
        public ImmutableOrderedSeq<T> Insert(T value) {
            var (l, r) = backing.Split(x => x >= new Key<T>(value));
            return new(l.Concat(r.Prepend(new(value))));
        }

        /// O(log n)
        public ImmutableOrderedSeq<T> Remove(T value) {
            var (l, r) = backing.Split(x => x >= new Key<T>(value));
            var (_, r2) = r.Split(x => x > new Key<T>(value));
            return new(l.Concat(r2));
        }

        /// Amortized theta(m log (n/m)) time (asymptotically optimal)
        public ImmutableOrderedSeq<T> MergeWith(ImmutableOrderedSeq<T> other) {
            return new(merge(backing, other.backing));
            
            static FTree<OrderedElem<T>, Key<T>> merge(FTree<OrderedElem<T>, Key<T>> xs, FTree<OrderedElem<T>, Key<T>> ys) {
                var view = FTree.toViewL(ys);
                if (!view.IsCons) return xs;
                // ReSharper disable once AccessToModifiedClosure // false positive
                var (l, r) = xs.Split(x => x > view.Head.Measure);
                return l.Concat(merge(view.Tail.Value, r).Prepend(view.Head));
            }
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

    internal readonly struct Key<T> : Measure<Key<T>>, IComparable<Key<T>> where T : IComparable<T> {
        
        public readonly bool HasValue;
        public readonly T Value;
        
        public Key(T value) {
            HasValue = true;
            Value = value;
        }

        public Key<T> Add(Key<T> other) => other.HasValue ? other : this; // always the highest key

        public int CompareTo(Key<T> other) {
            if (!HasValue) return -1;
            if (!other.HasValue) return 1;
            return Value.CompareTo(other.Value);
        }

        public static bool operator<(Key<T> left, Key<T> right) => left.CompareTo(right) < 0;
        public static bool operator>(Key<T> left, Key<T> right) => left.CompareTo(right) > 0;

        public static bool operator<=(Key<T> left, Key<T> right) => left.CompareTo(right) <= 0;
        public static bool operator>=(Key<T> left, Key<T> right) => left.CompareTo(right) >= 0;
    }

    internal readonly struct OrderedElem<T> : Measured<Key<T>> where T : IComparable<T>
    {
        public readonly T Value;
        public OrderedElem(T value) => Value = value;
        public Key<T> Measure => new(Value);
    }
}