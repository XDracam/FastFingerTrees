using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

// ReSharper disable ParameterHidesMember // shadowing is a friend, not a foe
// ReSharper disable VariableHidesOuterVariable

public static class ImmutableSeq
{
    public static ImmutableSeq<T> Create<T>(params ReadOnlySpan<T> values) => CreateRange(values.ToArray());
        
    public static ImmutableSeq<T> CreateRange<T>(IEnumerable<T> values)
    {
        var valuesArr = values.Select(v => new SeqElem<T>(v)).ToArray();
        return new(FTree<SeqElem<T>, Size>.CreateRange(valuesArr), valuesArr.Length);
    }
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
    private readonly int count;
    private readonly FTree<SeqElem<T>, Size> backing;
    
    internal ImmutableSeq(FTree<SeqElem<T>, Size> backing, int count) {
        this.backing = backing;
        this.count = count;
    }

    public static ImmutableSeq<T> Empty => new(FTree<SeqElem<T>, Size>.Empty, 0);

    // We could return the measure, but we don't want to
    //  force evaluation of everything just for the count.
    public int Count => count;

    private (FTree<SeqElem<T>, Size>, FTree<SeqElem<T>, Size>) splitAt(int idx) => 
        backing.Split(s => idx < s.Value);
    
    private (ILazy<FTree<SeqElem<T>, Size>>, ILazy<FTree<SeqElem<T>, Size>>) splitAtLazy(int idx) => 
        backing.SplitLazy(s => idx < s.Value);
        
    // O(log n)
    public (ImmutableSeq<T> Before, ImmutableSeq<T> After) SplitAt(int idx) {
        var (l, r) = splitAt(idx);
        return (new(l, idx), new(r, count - idx));
    }
        
    // O(log n)
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
            var (_, rem) = splitAtLazy(start);
            var newCount = count + start - end;
            var res = new ImmutableSeq<T>(rem.Value, newCount);
            if (end >= Count) return res;
            return new(res.splitAtLazy(end-start).Item1.Value, newCount);
        }
    }
        
    // Amortized O(1)
    public ImmutableSeq<T> Prepend(T element) => new(backing.Prepend(new(element)), count + 1);
        
    // Amortized O(1)
    public ImmutableSeq<T> Append(T element) => new(backing.Append(new(element)), count + 1);

    // O(1)
    public ref readonly T Head => ref backing.Head.Value;

    // O(1)
    public ref readonly T Last => ref backing.Last.Value;
        
    // Amortized O(1)
    public ImmutableSeq<T> Tail => new(backing.Tail, count - 1);
        
    // Amortized O(1)
    public ImmutableSeq<T> Init => new(backing.Init, count - 1);
        
    public ImmutableSeq<T> Add(T value) => new(backing.Append(new(value)), count + 1);

    // Amortized O(1)
    public ImmutableSeq<T> Concat(ImmutableSeq<T> items) => new(backing.Concat(items.backing), count + items.Count);
        
    // Amortized O(1) when items is another ImmutableSeq, else O(|items|)
    public ImmutableSeq<T> AddRange(IEnumerable<T> items) {
        if (items is ImmutableSeq<T> seq) 
            return new(backing.Concat(seq.backing), count + seq.count); // O(1)

        var itemsArray = items.Select(x => new SeqElem<T>(x)).ToArray();
        var otherTree = FTree<SeqElem<T>, Size>.CreateRange(itemsArray);
        return new(backing.Concat(otherTree), count + itemsArray.Length);
    }

    // O(log n), or amortized O(1) if appending or prepending
    public ImmutableSeq<T> Insert(int index, T element) {
        if (index == 0) return Prepend(element);
        if (index == Count) return Append(element);
        var (l, r) = splitAt(index);
        return new(l.Concat(r.Prepend(new(element))), count + 1);
    }

    // O(log n + |items|), or O(log n) if items is an <c>ImmutableSeq{T}</c>
    public ImmutableSeq<T> InsertRange(int index, IEnumerable<T> items) {
        var (l, r) = splitAt(index);
        if (items is ImmutableSeq<T> seq) 
            return new(l.Concat(seq.backing).Concat(r), count + seq.count);
        
        var itemsArr = items.Select(i => new SeqElem<T>(i)).ToArray();
        return new(l.Concat(FTree<SeqElem<T>, Size>.CreateRange(itemsArr)).Concat(r), count + itemsArr.Length);
    }

    // O(log n)
    public ImmutableSeq<T> RemoveRange(int index, int count) {
        var (l, tail) = splitAt(index);
        var (_, r) = tail.SplitLazy(s => count < s.Value);
        return new(l.Concat(r.Value), this.count - count);
    }

    // O(log n)
    public ImmutableSeq<T> RemoveAt(int index) {
        var (l, _, r) = backing.SplitTree(s => index < s.Value, new());
        return new(l.Concat(r), count - 1);
    }

    // O(log n)
    public ImmutableSeq<T> SetItem(int index, T value) {
        var (l, _, r) = backing.SplitTree(s => index < s.Value, new());
        return new(l.Append(new(value)).Concat(r), count);
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
        
    // These are hidden by default because they are comparatively slow. Don't use them. Consider a different collection.

    int IImmutableList<T>.IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) {
        equalityComparer ??= EqualityComparer<T>.Default;
        var idx = index;
        var target = backing.SplitLazy(s => idx < s.Value).Item2.Value
            .SplitLazy(s => count < s.Value).Item1.Value;
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
        var target = backing.SplitLazy(s => startIdx < s.Value).Item2.Value
            .SplitLazy(s => count < s.Value).Item1.Value;
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
    
// Note: I tried separating Ops from the measure type, and it changed absolutely nothing
internal readonly struct Size(int value) : IFTreeMeasure<Size>
{
    public readonly int Value = value;

    public static Size Add(in Size a, in Size b) => new(a.Value + b.Value);

    public static Size Add(in Size a, in Size b, in Size c) => new(a.Value + b.Value + c.Value);

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
    // TODO: if idx closer to end then use minus and go back for faster results
    //   (and custom versions of SplitTree etc that do this as well)
    
    // guaranteed to be O(logn)
    // like SplitTree, but doesn't generate unnecessary new trees
    // also should check for the first `i > target` and stop
    internal static ref readonly T LookupTree<T>(this FTree<T, Size> tree, int target) where T : IFTreeElement<Size> =>
        ref lookupTree(tree, target, 0, out _);
    
    // `initial` is the number of elements to the left of the current tree.
    // `i` is used as a tracker of the number of elements visited when the method returns.
    // This memory slot on the stack is shared across all recursive lookups.
    // Without this weird workaround, we wouldn't be able to ref return the result,
    //  which could hurt performance if T was a value type larger than a pointer.
    private static ref readonly T lookupTree<T>(this FTree<T, Size> tree, int target, int initial, out int i) 
        where T : IFTreeElement<Size>
    {
        i = initial;
        if (tree is FTree<T, Size>.Single s) 
            return ref s.Value;
            
        if (tree is FTree<T, Size>.Deep(var pr, var m, var sf)) {
            var vpr = i + pr.Measure.Value;
            if (vpr > target) 
                return ref lookupDigit(target, i, pr);

            i = vpr;
            var mValue = m.Value;
            var vm = vpr + mValue.Measure.Value;
            if (vm > target) {
                var xs = lookupTree(mValue, target, i, out i);
                return ref lookupDigit(target, i, xs);
            }

            i = vm;
            return ref lookupDigit(target, i, sf);
        }
        throw new InvalidOperationException();
            
        static ref readonly T lookupDigit(int target, int i, Digit<T, Size> digit) {
            var length = digit.Length;
            if (length == 1)
                return ref digit[0];
            for (var idx = 0; idx < length; ++idx) {
                ref readonly var curr = ref digit[idx];
                var newI = i + curr.Measure.Value;
                if (newI > target) return ref curr;
                i = newI;
            }
            return ref digit[length - 1];
        }
    }
    
    
}