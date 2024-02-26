using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace FTrees
{
    public static class ImmutableIntervalTree
    {
        public static ImmutableIntervalTree<T, P> Create<T, P>(
            params Interval<T, P>[] values
        ) where P : IComparable<P> => CreateRange(values);

        public static ImmutableIntervalTree<T, P> CreateRange<T, P>(
            IEnumerable<Interval<T, P>> values
        ) where P : IComparable<P> {
            var res = ImmutableIntervalTree<T, P>.Empty;
            foreach (var item in values) res = res.Insert(item);
            return res;
        }
    }
    
    /// <summary>
    /// An immutable interval tree backed by a (2,3)-Finger-Tree.
    /// All operations run in logarithmic time.
    /// Allows managing of objects with intervals. Overlapping intervals can be queried efficiently.
    /// Also allows efficient merging of two interval trees.
    /// </summary>
    public readonly struct ImmutableIntervalTree<T, P> where P : IComparable<P>
    {
        private readonly FTree<Interval<T, P>, KeyPrio<P>> backing;
        internal ImmutableIntervalTree(FTree<Interval<T, P>, KeyPrio<P>> backing) => this.backing = backing;
        
        public static ImmutableIntervalTree<T, P> Empty => new(FTree<Interval<T, P>, KeyPrio<P>>.Empty);

        /// O(log n)
        public ImmutableIntervalTree<T, P> Insert(Interval<T, P> interval) {
            var (l, r) = backing.Split(x => x.Greater(interval.Low));
            return new(l.Concat(r.Prepend(interval)));
        }

        /// Amortized Theta(m log (n/m)) time (asymptotically optimal)
        public ImmutableIntervalTree<T, P> MergeWith(ImmutableIntervalTree<T, P> other) {
            return new(merge(backing, other.backing));
            
            static FTree<Interval<T, P>, KeyPrio<P>> merge(
                FTree<Interval<T, P>, KeyPrio<P>> xs, 
                FTree<Interval<T, P>, KeyPrio<P>> ys
            ) {
                var view = FTree.toViewL(ys);
                if (!view.IsCons) return xs;
                // ReSharper disable once AccessToModifiedClosure // false positive
                var (l, r) = xs.Split(x => x.Key > view.Head.measure.Key);
                return l.Concat(merge(view.Tail.Value, r).Prepend(view.Head));
            }
        }

        /// Theta(log n)
        public bool TryGetAnyIntersecting(P low, P high, out Interval<T, P> intersecting) {
            (_, intersecting, _) = backing.SplitTree(x => x.AtLeast(low), default);
            return backing.Measure.AtLeast(low) && intersecting.Low.CompareTo(high) <= 0;
        }

        /// Theta(m log(n/m)) where m is result size
        public ImmutableStack<Interval<T, P>> AllIntersecting(P low, P high) {
            return matches(backing.TakeUntil(x => x.Greater(high)));
            
            ImmutableStack<Interval<T, P>> matches(FTree<Interval<T, P>, KeyPrio<P>> xs) {
                var view = FTree.toViewL(xs.DropUntil(x => x.AtLeast(low)));
                if (!view.IsCons) return ImmutableStack<Interval<T, P>>.Empty;
                return matches(view.Tail.Value).Push(view.Head);
            }
        }
    }
    
    public readonly struct Interval<T, P> : Measured<KeyPrio<P>> where P : IComparable<P>
    {
        public readonly T Value;
        public readonly P Low, High; // inclusive
        public Interval(T value, P low, P high) => 
            (Value, Low, High) = (value, low, high);

        KeyPrio<P> Measured<KeyPrio<P>>.Measure => measure;
        internal KeyPrio<P> measure => new(new Key<P>(Low), new Prio<P>(High));
    }

    internal readonly struct KeyPrio<P> : Measure<KeyPrio<P>> where P : IComparable<P>
    {
        public readonly Key<P> Key;
        public readonly Prio<P> Prio;
        public KeyPrio(Key<P> key, Prio<P> prio) => (Key, Prio) = (key, prio);

        public KeyPrio<P> Add(KeyPrio<P> other) => new(Key.Add(other.Key), Prio.Add(other.Prio));

        public bool AtLeast(P k) => new Prio<P>(k).CompareTo(Prio) <= 0;
        public bool Greater(P k) => Key.CompareTo(new(k)) > 0;
    }
}