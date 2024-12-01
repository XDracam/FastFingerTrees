using System;
using System.Collections;
using System.Collections.Generic;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

public abstract partial class FTree<T, V>
{
    internal sealed class EmptyT : FTree<T, V>
    {
        private EmptyT() { }
        public static readonly EmptyT Instance = new();
        public override V Measure => default;

        internal override View<T, V> toViewL() => new();
        internal override View<T, V> toViewR() => new();

        public override TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) => other;
        public override TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) => other;

        public override FTree<T, V> Prepend(T toAdd) => new Single(toAdd);
        public override FTree<T, V> Append(T toAdd) => new Single(toAdd);

        public override ref readonly T Head => throw new InvalidOperationException("Empty FTree has no head");
        public override ref readonly T Last => throw new InvalidOperationException("Empty FTree has no last");

        public override (FTree<T, V>, T, FTree<T, V>) SplitTree(Func<V, bool> p, V i) => 
            throw new InvalidOperationException("Cannot split an empty FTree");

        public override (ILazy<FTree<T, V>>, T, ILazy<FTree<T, V>>) SplitTreeLazy(Func<V, bool> p, V i) =>
            throw new InvalidOperationException("Cannot split an empty FTree");

        public override IEnumerator<T> GetEnumerator() => new EmptyEnumerator();
        public override IEnumerator<T> GetReverseEnumerator() => new EmptyEnumerator();
        
        private sealed class EmptyEnumerator : IEnumerator<T>
        {
            public bool MoveNext() => false;
            public void Reset() { }
            T IEnumerator<T>.Current => default;
            object IEnumerator.Current => default;
            public void Dispose() { }
        }
    }
}