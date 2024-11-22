using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

public static class ImmutableSeq
{
    public static ImmutableSeq<T> Create<T>(params ReadOnlySpan<T> values) => CreateRange(values.ToArray());
        
    public static ImmutableSeq<T> CreateRange<T>(IEnumerable<T> values) =>
        new(FTree<SeqElem<T>, Size>.CreateRange(values.Select(v => new SeqElem<T>(v)).ToArray()));
}
    
/// <summary>
/// Immutable indexed sequence backed by a (2,3)-Finger-Tree.
/// All operations directly available run in either logarithmic or amortized constant time.
/// 
/// Inserting <see cref="IEnumerable{T}"/> will have no overhead if the enumerable is already an <c>ImmutableSeq{T}</c>.
/// Otherwise a seq will need to be built, which takes O(|items|) amortized time.
/// </summary>
[CollectionBuilder(typeof(ImmutableSeq), nameof(ImmutableSeq.Create))]
public readonly struct ImmutableSeq<T> : IImmutableList<T>
{
    private readonly FTree<SeqElem<T>, Size> backing;
    internal ImmutableSeq(FTree<SeqElem<T>, Size> backing) => this.backing = backing;

    public static ImmutableSeq<T> Empty => new(FTree<SeqElem<T>, Size>.Empty);

    /// Amortized O(1)
    public int Count => backing.Measure.Value;

    private (FTree<SeqElem<T>, Size>, FTree<SeqElem<T>, Size>) splitAt(int idx) => 
        backing.Split(s => idx < s.Value);
        
    /// O(log n)
    public (ImmutableSeq<T> Before, ImmutableSeq<T> After) SplitAt(int idx) {
        var (l, r) = splitAt(idx);
        return (new(l), new(r));
    }
        
    /// O(log n)
    public ref readonly T this[int idx] {
        get {
            if (idx < 0) throw new IndexOutOfRangeException();
            if (idx >= Count) throw new IndexOutOfRangeException();
            if (idx == 0) return ref Head;
            if (idx == Count - 1) return ref Last;

            return ref backing.LookupTree(idx).Value;
        }
    }
    
   T IReadOnlyList<T>.this[int idx] => this[idx]; // copy, not ref :c

   public ref readonly T this[Index index] => ref this[index.GetOffset(Count)];

    public ImmutableSeq<T> this[Range range] {
        get {
            var start = range.Start.GetOffset(Count);
            var end = range.End.GetOffset(Count);
            var (_, rem) = splitAt(start);
            var res = new ImmutableSeq<T>(rem);
            if (end >= Count) return res;
            return new(res.splitAt(end-start).Item1);
        }
    }

    // TODO: append and prepend could be faster (paper: "sometimes faster computing the size using subtraction")
        
    /// Amortized O(1)
    public ImmutableSeq<T> Prepend(T element) => new(backing.Prepend(new(element)));
        
    /// Amortized O(1)
    public ImmutableSeq<T> Append(T element) => new(backing.Append(new(element)));

    /// O(1)
    public ref readonly T Head => ref backing.Head.Value;

    /// O(1)
    public ref readonly T Last => ref backing.Last.Value;
        
    /// Amortized O(1)
    public ImmutableSeq<T> Tail => new(backing.Tail);
        
    /// Amortized O(1)
    public ImmutableSeq<T> Init => new(backing.Init);
        
    public ImmutableSeq<T> Add(T value) => new(backing.Append(new(value)));

    /// Amortized O(1)
    public ImmutableSeq<T> Concat(ImmutableSeq<T> items) => new(backing.Concat(items.backing));
        
    /// Amortized O(1) when items is another ImmutableSeq, else O(|items|)
    public ImmutableSeq<T> AddRange(IEnumerable<T> items) {
        if (items is ImmutableSeq<T> seq) 
            return new(backing.Concat(seq.backing)); // O(1)
            
        var otherTree = FTree<SeqElem<T>, Size>.CreateRange(items.Select(x => new SeqElem<T>(x)).ToArray());
        return new(backing.Concat(otherTree));
    }

    /// O(log n), or amortized O(1) if appending or prepending
    public ImmutableSeq<T> Insert(int index, T element) {
        // TODO: this is lower than ImmutableArrays...
        if (index == 0) return Prepend(element);
        if (index == Count) return Append(element);
        var (l, r) = splitAt(index);
        return new(l.Concat(r.Prepend(new(element))));
    }

    /// O(log n + |items|), or O(log n) if items is an <c>ImmutableSeq{T}</c>
    public ImmutableSeq<T> InsertRange(int index, IEnumerable<T> items) {
        var (l, r) = splitAt(index);
        var middle = items is ImmutableSeq<T> seq 
            ? seq.backing 
            : FTree<SeqElem<T>, Size>.CreateRange(items.Select(i => new SeqElem<T>(i)).ToArray());
        return new(l.Concat(middle).Concat(r));
    }

    /// O(log n)
    public ImmutableSeq<T> RemoveRange(int index, int count) {
        var (l, tail) = splitAt(index);
        var (_, r) = tail.Split(s => count < s.Value);
        return new(l.Concat(r));
    }

    /// O(log n)
    public ImmutableSeq<T> RemoveAt(int index) {
        var (l, _, r) = backing.SplitTree(s => index < s.Value, new());
        return new(l.Concat(r));
    }

    /// O(log n)
    public ImmutableSeq<T> SetItem(int index, T value) {
        var (l, _, r) = backing.SplitTree(s => index < s.Value, new());
        return new(l.Append(new(value)).Concat(r));
    }

    #region Enumerator
        
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public Enumerator GetEnumerator() => new(backing.GetEnumerator());

    public readonly struct Enumerator : IEnumerator<T>
    {
        private readonly IEnumerator<SeqElem<T>> backing;
        internal Enumerator(IEnumerator<SeqElem<T>> backing) => this.backing = backing;

        public T Current => backing.Current.Value;
        object IEnumerator.Current => Current;
            
        public bool MoveNext() => backing.MoveNext();
        public void Reset() => backing.Reset();

        public void Dispose() => backing.Dispose();
    }
        
    #endregion
        
    #region IImmutableList API
        
    IImmutableList<T> IImmutableList<T>.Add(T value) => Add(value);
    IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items) => AddRange(items);
    IImmutableList<T> IImmutableList<T>.Insert(int index, T element) => Insert(index, element);
    IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items) => InsertRange(index, items);
    IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count) => RemoveRange(index, count);
    IImmutableList<T> IImmutableList<T>.RemoveAt(int index) => RemoveAt(index);
    IImmutableList<T> IImmutableList<T>.SetItem(int index, T value) => SetItem(index, value);
    IImmutableList<T> IImmutableList<T>.Clear() => Empty;
        
    // These are hidden by default because they are slow. Don't use them. Consider a different collection.

    int IImmutableList<T>.IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) {
        equalityComparer ??= EqualityComparer<T>.Default;
        var idx = index;
        var target = backing.Split(s => idx < s.Value).Item2.Split(s => count < s.Value).Item1;
        using var it = target.GetEnumerator();
        while (it.MoveNext()) {
            if (equalityComparer.Equals(it.Current.Value, item)) 
                return index;
            index += 1;
        }
        return -1;
    }
        
    int IImmutableList<T>.LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) {
        // index is the END of the interval...
        equalityComparer ??= EqualityComparer<T>.Default;
        var startIdx = index - count + 1;
        var target = backing.Split(s => startIdx < s.Value).Item2.Split(s => count < s.Value).Item1;
        using var it = target.GetReverseEnumerator();
        while (it.MoveNext()) {
            if (equalityComparer.Equals(it.Current.Value, item)) 
                return index;
            index -= 1;
        }
        return -1;
    }

    IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T> equalityComparer) {
        var self = (IImmutableList<T>)this;
        var idx = self.IndexOf(value, equalityComparer);
        if (idx == -1) return self;
        return RemoveAt(idx);
    }

    private ImmutableSeq<T> removeFirst(Predicate<T> match, out bool didRemove) {
        using var it = GetEnumerator();
        var index = 0;
        while (it.MoveNext()) {
            if (match(it.Current)) {
                didRemove = true;
                return RemoveAt(index);
            }
            index += 1;
        }

        didRemove = false;
        return this;
    }

    IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match) {
        var res = this;
        while (true) {
            res = res.removeFirst(match, out var didRemove);
            if (!didRemove) return res;
        }
    }

    IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer) {
        var self = (IImmutableList<T>)this;
        foreach (var item in items) 
            self = self.Remove(item);
        return self;
    }

    IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) {
        var self = (IImmutableList<T>)this;
        var idx = self.IndexOf(oldValue, equalityComparer);
        if (idx == -1) return self;
        return SetItem(idx, newValue);
    }
        
    #endregion
}
    
internal readonly struct Size(int value) : IFTreeMeasure<Size>
{
    public readonly int Value = value;

    public static Size Add(params ReadOnlySpan<Size> values) {
        var sum = 0;
        for (var i = 0; i < values.Length; ++i) 
            sum += values[i].Value;
        return new(sum);
    }

    public static Size Add<T>(ReadOnlySpan<T> values) where T : IFTreeElement<Size> {
        var sum = 0;
        for (var i = 0; i < values.Length; ++i) 
            sum += values[i].Measure.Value;
        return new(sum);
    }
}

internal readonly struct SeqElem<T>(T value) : IFTreeElement<Size>
{
    public readonly T Value = value;
    public Size Measure => new(1);
}

internal static class ImmutableSeqUtils
{

    // guaranteed to be O(logn)
    // like SplitTree, but doesn't generate unnecessary new trees
    // also should check for the first `i > target` and stop
    internal static ref readonly T LookupTree<T>(this FTree<T, Size> tree, int target) where T : IFTreeElement<Size> {
        // The C# lifetime analyzer cannot prove that a local variable will not be included in the returned T somehow.
        // As a consequence, it cannot allow stack memory to leak => we need to heap allocate this.
        var cell = new[]{0}; 
        return ref lookupTree(tree, target, ref cell[0]);
    }
    
    // i is maintained as a memory slot and updated in the recursion
    private static ref readonly T lookupTree<T>(this FTree<T, Size> tree, int target, ref int i) 
        where T : IFTreeElement<Size>
    {
        if (tree is FTree<T, Size>.Single s) 
            return ref s.Value;
            
        if (tree is FTree<T, Size>.Deep(var pr, var m, var sf)) {
            var vpr = i + pr.Measure.Value;
            if (vpr > target) 
                return ref lookupDigit(target, i, pr.Values.AsSpan());

            i = vpr;
            var mValue = m.Value;
            var vm = vpr + mValue.Measure.Value;
            if (vm > target) {
                var xs = lookupTree(mValue, target, ref i);
                return ref lookupNode(target, i, xs);
            }

            i = vm;
            return ref lookupDigit(target, i, sf.Values.AsSpan());
        }
        throw new InvalidOperationException();
            
        static ref readonly T lookupNode(int target, int i, Node<T, Size> node) {
            ref readonly var fst = ref node.First;
            var i1 = i + fst.Measure.Value;
            if (i1 > target) 
                return ref fst;
            ref readonly var snd = ref node.Second;
            var i2 = i1 + snd.Measure.Value;
            if (!node.HasThird || i2 > target) 
                return ref snd;
            return ref node.Third;
        }
            
        static ref readonly T lookupDigit(int target, int i, ReadOnlySpan<T> digit) {
            if (digit.Length == 1)
                return ref digit[0];
            for (var idx = 0; idx < digit.Length; ++idx) {
                ref readonly var curr = ref digit[idx];
                var newI = i + curr.Measure.Value;
                if (newI.CompareTo(target) > 0) return ref curr;
                i = newI;
            }
            return ref digit[^1];
        }
    }
    
    
}