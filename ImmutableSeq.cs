using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FTrees
{
    public static class ImmutableSeq
    {
        public static ImmutableSeq<T> Create<T>(params T[] values) => CreateRange(values);
        
        public static ImmutableSeq<T> CreateRange<T>(IEnumerable<T> values) =>
            new(FTree<SeqElem<T>, Size>.CreateRange(values.Select(v => new SeqElem<T>(v))));
    }
    
    /// <summary>
    /// Immutable indexed sequence backed by a (2,3)-Finger-Tree.
    /// All operations directly available run in either logarithmic or amortized constant time.
    /// 
    /// Inserting <see cref="IEnumerable{T}"/> will have no overhead if the enumerable is already an <c>ImmutableSeq{T}</c>.
    /// Otherwise a seq will need to be built, which takes O(|items|) amortized time.
    /// </summary>
    public readonly struct ImmutableSeq<T> : IImmutableList<T>
    {
        private readonly FTree<SeqElem<T>, Size> backing;
        internal ImmutableSeq(FTree<SeqElem<T>, Size> backing) => this.backing = backing;

        public static ImmutableSeq<T> Empty => new(FTree<SeqElem<T>, Size>.Empty);

        /// Amortized O(1)
        public int Count => backing.Measure().Value;

        private (FTree<SeqElem<T>, Size>, FTree<SeqElem<T>, Size>) splitAt(int idx) => backing.Split(s => idx < s.Value);
        
        /// O(log n)
        public (ImmutableSeq<T> Before, ImmutableSeq<T> After) SplitAt(int idx) {
            var (l, r) = splitAt(idx);
            return (new(l), new(r));
        }

        
        /// O(log n)
        public T this[int idx] {
            get {
                if (idx == 0) return Head;
                if (idx == Count - 1) return Last;
                if (idx < 0) throw new IndexOutOfRangeException();
                if (idx >= Count) throw new IndexOutOfRangeException();
                return backing.LookupTree(s => idx < s.Value, default).Value.Value;
            }
        }

        // TODO: append and prepend could be faster (paper: "sometimes faster computing the size using subtraction")
        
        /// Amortized O(1)
        public ImmutableSeq<T> Prepend(T element) => new(backing.Prepend(new(element)));
        
        /// Amortized O(1)
        public ImmutableSeq<T> Append(T element) => new(backing.Append(new(element)));

        /// O(1)
        public T Head => backing.Head.Value;

        /// O(1)
        public T Last => backing.Last.Value;
        
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
            var res = backing;
            foreach (var item in items) 
                res = res.Append(new(item));
            return new(res);
        }

        /// O(log n), or amortized O(1) if appending or prepending
        public ImmutableSeq<T> Insert(int index, T element) {
            if (index == 0) return Prepend(element);
            if (index == Count) return Append(element);
            var (l, r) = splitAt(index);
            return new(l.Append(new(element)).Concat(r));
        }

        /// O(log n + |items|), or O(log n) if items is an <c>ImmutableSeq{T}</c>
        public ImmutableSeq<T> InsertRange(int index, IEnumerable<T> items) {
            var (l, r) = splitAt(index);
            var middle = items is ImmutableSeq<T> seq 
                ? seq.backing 
                : FTree<SeqElem<T>, Size>.CreateRange(items.Select(i => new SeqElem<T>(i)), isLazy: true);
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
            var (l, _, r) = backing.SplitTree(s => index < s.Value, default);
            return new(l.Concat(r));
        }

        /// O(log n)
        public ImmutableSeq<T> SetItem(int index, T value) {
            var (l, _, r) = backing.SplitTree(s => index < s.Value, default);
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
    
    internal readonly struct Size : Measure<Size>
    {
        public readonly int Value;
        public Size(int value) => Value = value;
        public Size Add(Size other) => new(Value + other.Value);
    }

    internal readonly struct SeqElem<T> : Measured<Size>
    {
        public readonly T Value;
        public SeqElem(T value) => Value = value;
        public Size Measure => new(1);
    }

}