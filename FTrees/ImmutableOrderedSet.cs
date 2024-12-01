using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

public static class ImmutableOrderedSet
{
    public static ImmutableOrderedSet<T> Create<T>(params ReadOnlySpan<T> values) 
    where T : IComparable<T>, IEquatable<T> {
        var res = ImmutableOrderedSet<T>.Empty;
        foreach (var item in values) 
            res = res.Add(item);
        return res;
    }
        
    public static ImmutableOrderedSet<T> CreateRange<T>(IEnumerable<T> values) 
    where T : IComparable<T>, IEquatable<T> {
        var res = ImmutableOrderedSet<T>.Empty;
        foreach (var item in values) 
            res = res.Add(item);
        return res;
    }
}
    
/// <summary>
/// An immutable ordered sequence backed by a (2,3)-Finger-Tree.
/// Keeps all elements sorted at all times. Can be iterated efficiently.
/// All operations run in logarithmic time.
/// Also allows efficient merging and partitioning in logarithmic time, unlike <see cref="ImmutableSortedSet{T}"/>.
/// </summary>
[CollectionBuilder(typeof(ImmutableOrderedSet), nameof(ImmutableOrderedSet.Create))]
public readonly struct ImmutableOrderedSet<T> : IImmutableSet<T> where T : IComparable<T>, IEquatable<T>
{
    // Note: no support for IComparer: how would we even cache those? in all keys? therefore all elements?
    
    public int Count { get; }
    private readonly FTree<OrderedElem<T>, Key<T>> backing;
    
    private ImmutableOrderedSet(FTree<OrderedElem<T>, Key<T>> backing, int count) => 
        (this.backing, this.Count) = (backing, count);
        
    public static readonly ImmutableOrderedSet<T> Empty = new(FTree<OrderedElem<T>, Key<T>>.Empty, 0);
    
    public bool IsEmpty => backing.IsEmpty;

    // O(log n)
    // Could return ImmutableOrderedSet, but then we'd 
    //  lose performance due to maintaining the count...
    public (IEnumerable<T> Less, IEnumerable<T> Greater) Partition(T k) {
        var cmpKey = new Key<T>(k);
        var (l, r) = backing.Split(x => x >= cmpKey);
        return (l.Select(x => x.Value), r.Select(x => x.Value));
    }

    // O(log n)
    public ImmutableOrderedSet<T> Add(T value) {
        if (backing.IsEmpty) 
            return new(new FTree<OrderedElem<T>, Key<T>>.Single(new(value)), 1);
        var (l, x, r) = backing.SplitTree(x => x >= new Key<T>(value), new());
        return x.Measure.Value.CompareTo(value) switch {
            < 0 => new(l.Append(x).Append(new(value)).Concat(r), 1),
            0 => this, // already present
            > 0 => new(l.Append(new(value)).Append(x).Concat(r), 1)
        };
    }

    // O(log n)
    public ImmutableOrderedSet<T> Remove(T value, out bool wasRemoved) {
        wasRemoved = Contains(value);
        if (!wasRemoved) return this;
        var (l, r) = backing.Split(x => x >= new Key<T>(value));
        var (elem, r2) = r.Split(x => x > new Key<T>(value));
        return new(l.Concat(r2), Count - elem.Count());
    }

    /// O(log n)
    public ImmutableOrderedSet<T> Remove(T value) => Remove(value, out _);
    
    /// Amortized theta(m log (n/m)) time (asymptotically optimal)
    public ImmutableOrderedSet<T> Union(ImmutableOrderedSet<T> other) {
        return new(merge(backing, other.backing), Count + other.Count);
            
        static FTree<OrderedElem<T>, Key<T>> merge(
            FTree<OrderedElem<T>, Key<T>> xs, 
            FTree<OrderedElem<T>, Key<T>> ys
        ) {
            if (xs.IsEmpty) return ys;
            if (ys.IsEmpty) return xs;
                
            var view = FTreeImplUtils.toViewL(ys);
                
            // ReSharper disable once AccessToModifiedClosure // false positive
            var (l, x, r) = xs.SplitTree(x => x >= view.Head.Measure, new());
            if (x.Measure == view.Head.Measure)
                return l.Concat(merge(view.Tail, r).Prepend(x));
            return l.Concat(merge(view.Tail, r.Prepend(x)).Prepend(view.Head));
        }
    }

    public bool Contains(T value) {
        var newKey = new Key<T>(value);
        var i = new Key<T>();
        var found = backing.LookupTree(ref newKey, ref i);
        return EqualityComparer<T>.Default.Equals(found.Value, value);
    }
    
    public IImmutableSet<T> Union(IEnumerable<T> other) {
        if (other is ImmutableOrderedSet<T> ios)
            return Union(ios);
        var res = this;
        foreach (var x in other) 
            res = res.Add(x);
        return res;
    }
    
#region IImmutableList<T> impl
    // These are hidden because they are not optimal, but can still be used for compatibility.
    
    IImmutableSet<T> IImmutableSet<T>.Add(T value) => Add(value);
    
    IImmutableSet<T> IImmutableSet<T>.Clear() => Empty;

    bool IImmutableSet<T>.Overlaps(IEnumerable<T> other) => other.Any(Contains);

    IImmutableSet<T> IImmutableSet<T>.Remove(T value) => Remove(value);
    
    bool IImmutableSet<T>.TryGetValue(T equalValue, out T actualValue) {
        var newKey = new Key<T>(equalValue);
        var i = new Key<T>();
        var found = backing.LookupTree(ref newKey, ref i);
        actualValue = found.Value;
        return EqualityComparer<T>.Default.Equals(equalValue, actualValue);
    }

    bool IImmutableSet<T>.SetEquals(IEnumerable<T> other) {
        var res = this;
        foreach (var x in other) {
            res = res.Remove(x, out var removed);
            if (!removed) return false;
        }
        return res.IsEmpty;
    }

    IImmutableSet<T> IImmutableSet<T>.SymmetricExcept(IEnumerable<T> other) {
        throw new NotImplementedException();
    }

    IImmutableSet<T> IImmutableSet<T>.Except(IEnumerable<T> other) {
        var res = this;
        foreach (var x in other) 
            res = res.Remove(x);
        return res;
    }

    IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other) {
        var res = Empty;
        foreach (var x in other) if (!Contains(x)) res = res.Add(x);
        return res;
    }

    bool IImmutableSet<T>.IsProperSubsetOf(IEnumerable<T> other) {
        var res = this;
        var foundMissing = false;
        foreach (var x in other) {
            res = res.Remove(x, out var removed);
            if (!removed) foundMissing = true;
        }
        return res.IsEmpty && foundMissing;
    }

    bool IImmutableSet<T>.IsProperSupersetOf(IEnumerable<T> other) {
        var res = this;
        foreach (var x in other) {
            res = res.Remove(x, out var removed);
            if (!removed) return false;
        }
        return !res.IsEmpty;
    }

    bool IImmutableSet<T>.IsSubsetOf(IEnumerable<T> other) {
        var res = this;
        foreach (var x in other) 
            res = res.Remove(x);
        return res.IsEmpty;
    }

    bool IImmutableSet<T>.IsSupersetOf(IEnumerable<T> other) => other.All(Contains);

#endregion
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

internal static class ImmutableOrderedSetUtils 
{
    // guaranteed to be O(logn)
    // like SplitTree, but doesn't generate unnecessary new trees
    // also should check for the first `i > target` and stop
    internal static ref readonly T LookupTree<T, TKey>(
        this FTree<T, TKey> tree, 
        ref TKey target, 
        ref TKey i
    ) where T : IFTreeElement<TKey> where TKey : struct, IFTreeMeasure<TKey>, IWithComparisons<TKey> {
        if (tree is FTree<T, TKey>.Single s) return ref s.Value;
        if (tree is FTree<T, TKey>.Deep(var pr, var m, var sf)) {
            var vpr = TKey.Add(i, pr.Measure);
            if (vpr >= target) 
                return ref lookupDigit(ref target, ref i, pr.Values);

            i = vpr;
            var mValue = m.Value;
            var vm = TKey.Add(vpr, mValue.Measure);
            if (vm >= target) {
                var xs = LookupTree(mValue, ref target, ref i);
                return ref lookupDigit(ref target, ref i, xs.Values);
            }

            i = vm;
            return ref lookupDigit(ref target, ref i, sf.Values);
        }
        throw new InvalidOperationException();
            
        static ref readonly T lookupDigit(ref TKey target, ref TKey i, ReadOnlySpan<T> digit) {
            if (digit.Length == 1)
                return ref digit[0];
            for (var idx = 0; idx < digit.Length; ++idx) {
                ref readonly var curr = ref digit[idx];
                var newI = TKey.Add(i, curr.Measure);
                if (newI >= target) return ref curr;
                i = newI;
            }
            return ref digit[^1];
        }
    }
}

internal interface IWithComparisons<T> 
    : IEquatable<T>, IComparable<T> where T : struct, IWithComparisons<T>
{
    static abstract bool operator>(in T a, in T b);
    static abstract bool operator<(in T a, in T b);
    static abstract bool operator>=(in T a, in T b);
    static abstract bool operator<=(in T a, in T b);
    static abstract bool operator==(in T a, in T b);
    static abstract bool operator!=(in T a, in T b);
}

internal readonly struct Key<T>(T value) 
    : IFTreeMeasure<Key<T>>, IWithComparisons<Key<T>> 
    where T : IComparable<T>
{
    public readonly T Value = value;
    public readonly bool HasValue = true;

    public static Key<T> Add(in Key<T> a, in Key<T> b) => b.HasValue ? b : a;
    public static Key<T> Add(in Key<T> a, in Key<T> b, in Key<T> c) => c.HasValue ? c : b.HasValue ? b : a;

    public static Key<T> Add(params ReadOnlySpan<Key<T>> values) {
        for (var i = values.Length - 1; i >= 0; --i) {
            ref readonly var curr = ref values[i];
            if (curr.HasValue) return curr;
        }
        return default;
    }

    public static Key<T> Add<A>(ReadOnlySpan<A> values) where A : IFTreeElement<Key<T>> {
        for (var i = values.Length - 1; i >= 0; --i) {
            var curr = values[i].Measure;
            if (curr.HasValue) return curr;
        }
        return default;
    }

    public int CompareTo(Key<T> other) {
        if (!HasValue) return -1;
        if (!other.HasValue) return 1;
        return Value.CompareTo(other.Value);
    }

    public static bool operator <(in Key<T> left, in Key<T> right) =>
        !left.HasValue || (right.HasValue && left.Value.CompareTo(right.Value) < 0);

    public static bool operator >(in Key<T> left, in Key<T> right) =>
        !right.HasValue || (left.HasValue && left.Value.CompareTo(right.Value) > 0);

    public static bool operator<=(in Key<T> left, in Key<T> right) => 
        !left.HasValue || !right.HasValue || left.Value.CompareTo(right.Value) <= 0;
    
    public static bool operator>=(in Key<T> left, in Key<T> right) => 
        !right.HasValue || !left.HasValue || left.Value.CompareTo(right.Value) >= 0;

    public static bool operator ==(in Key<T> a, in Key<T> b) =>
        a.HasValue == b.HasValue && EqualityComparer<T>.Default.Equals(a.Value, b.Value);
    
    public static bool operator!=(in Key<T> a, in Key<T> b) => 
        a.HasValue != b.HasValue || !EqualityComparer<T>.Default.Equals(a.Value, b.Value);

    public bool Equals(Key<T> other) => this == other;
    public override bool Equals(object obj) => obj is Key<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(HasValue, Value);
}

internal readonly struct OrderedElem<T>(T value) 
    : IFTreeElement<Key<T>> where T : IComparable<T>, IEquatable<T>
{
    public readonly T Value = value;
    public Key<T> Measure => new(Value);
}