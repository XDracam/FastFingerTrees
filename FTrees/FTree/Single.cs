using System;
using System.Collections;
using System.Collections.Generic;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

public abstract partial class FTree<T, V>
{
    internal sealed class Single(T value) : FTree<T, V>
    {
        public readonly T Value = value;
        public void Deconstruct(out T value) { value = Value; }
        public override V Measure => Value.Measure;

        internal override View<T, V> toViewL() => new(Value, EmptyT.Instance);
        internal override View<T, V> toViewR() => new(Value, EmptyT.Instance);
        
        public override TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) => reduceOp(Value, other);
        public override TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) => reduceOp(other, Value);

        public override FTree<T, V> Prepend(T toAdd) => 
            new Deep(
                new Digit<T, V>(toAdd), 
                FTree<Digit<T, V>, V>.EmptyT.LazyInstance, 
                new Digit<T, V>(Value)
            );

        public override FTree<T, V> Append(T toAdd) => 
            new Deep(
                new Digit<T, V>(Value), 
                FTree<Digit<T, V>, V>.EmptyT.LazyInstance, 
                new Digit<T, V>(toAdd)
            );

        public override ref readonly T Head => ref Value;
        public override ref readonly T Last => ref Value;

        public override (FTree<T, V>, T, FTree<T, V>) SplitTree(Func<V, bool> p, V i) => 
            (Empty, Value, Empty);
        
        public override (ILazy<FTree<T, V>>, T, ILazy<FTree<T, V>>) SplitTreeLazy(Func<V, bool> p, V i) => 
            (EmptyT.LazyInstance, Value, EmptyT.LazyInstance);

        public override IEnumerator<T> GetEnumerator() => new SingleEnumerator(Value);
        public override IEnumerator<T> GetReverseEnumerator() => new SingleEnumerator(Value);

        private sealed class SingleEnumerator : IEnumerator<T>
        {
            public readonly T Value;
            private bool hasMoved = false;

            public SingleEnumerator(T value) { Value = value; }

            public bool MoveNext() {
                if (hasMoved) return false;
                return hasMoved = true;
            }

            public void Reset() => hasMoved = false;

            T IEnumerator<T>.Current => Value;
            object IEnumerator.Current => Value;

            public void Dispose() { }
        }
    }
}