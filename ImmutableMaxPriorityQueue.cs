using System;
using System.Collections.Generic;
using System.Linq;

namespace FTrees
{
    public static class ImmutableMaxPriorityQueue
    {
        public static ImmutableMaxPriorityQueue<T, P> Create<T, P>(params (T, P)[] values) where P : IComparable<P> => 
            CreateRange(values);
        
        public static ImmutableMaxPriorityQueue<T, P> CreateRange<T, P>(IEnumerable<(T, P)> values) where P : IComparable<P> =>
            new(FTree<PrioElem<T, P>, Prio<P>>.CreateRange(values.Select(v => new PrioElem<T, P>(v.Item1, v.Item2))));
    }
    
    /// <summary>
    /// An immutable max priority queue backed by a (2,3)-Finger-Tree.
    /// Enqueueing works in amortized constant time. Extracting the max runs in O(log n).
    /// </summary>
    /// <remarks>
    /// If you enqueue an object twice with different priorities, then it will be there twice.
    /// There is currently no way of removing an element efficiently.
    /// It can be implemented, but only by also providing the elements' exact previous priority.
    /// </remarks>
    public readonly struct ImmutableMaxPriorityQueue<T, P> where P : IComparable<P>
    {
        // Note: no support for equality comparer: how would we even cache those? in all priorities? therefore all elements?
        
        private readonly FTree<PrioElem<T, P>, Prio<P>> backing;
        internal ImmutableMaxPriorityQueue(FTree<PrioElem<T, P>, Prio<P>> backing) => this.backing = backing;

        public static ImmutableMaxPriorityQueue<T, P> Empty => new(FTree<PrioElem<T, P>, Prio<P>>.Empty);

        // Note: element could already be present with different priority
        /// amortized O(1)
        public ImmutableMaxPriorityQueue<T, P> Enqueue(T element, P priority) =>
            new(backing.Append(new(element, priority)));

        /// O(log n)
        public ImmutableMaxPriorityQueue<T, P> ExtractMax(out T element, out P priority) {
            var tree = backing;
            var (l, x, r) = backing.SplitTree(x => tree.Measure() <= x, default);
            element = x.Value;
            priority = x.Priority;
            return new(l.Concat(r));
        }

        // Notes from paper:
        // 1. Could implement "any element >= k" in theta(log n) time
        // 2. Could implement "all elements >= k" in theta(m log(n/m)) time
    }
    
    internal readonly struct Prio<P> : Measure<Prio<P>>, IComparable<Prio<P>> where P : IComparable<P> 
    {
        public readonly bool HasValue;
        public readonly P Value; // none is "negative infinity"
        
        public Prio(P value) {
            HasValue = true;
            Value = value;
        }

        public Prio<P> Add(Prio<P> other) {
            return HasValue
                ? other.HasValue
                    ? new(Value.CompareTo(other.Value) >= 0 ? Value : other.Value)
                    : this
                : other;
        }

        public int CompareTo(Prio<P> other) {
            if (!HasValue) return -1;      // other is greater
            if (!other.HasValue) return 1; // this is greater
            return Value.CompareTo(other.Value);
        }
        
        public static bool operator<=(Prio<P> left, Prio<P> right) => left.CompareTo(right) <= 0;
        public static bool operator>=(Prio<P> left, Prio<P> right) => left.CompareTo(right) >= 0;
    }

    internal readonly struct PrioElem<T, P> : Measured<Prio<P>> where P : IComparable<P>
    {
        public readonly T Value;
        public readonly P Priority;
        public PrioElem(T value, P priority) => (Value, Priority) = (value, priority);
        public Prio<P> Measure => new(Priority);
    }
}