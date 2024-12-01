using System;
using System.Collections.Generic;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

using static FTreeImplUtils;

// ReSharper disable VariableHidesOuterVariable

public abstract partial class FTree<T, V>
{
    internal sealed class Deep(
        Digit<T, V> left,
        ILazy<FTree<Digit<T, V>, V>> spine,
        Digit<T, V> right
    ) : FTree<T, V>
    {
        // inlined lazy to save an allocation
        private V _measure;
        private bool _hasMeasure;
        
        public readonly Digit<T, V> Left = left;
        public readonly Digit<T, V> Right = right;
        public readonly ILazy<FTree<Digit<T, V>, V>> SpineLazy = spine;
        
        public FTree<Digit<T, V>, V> Spine => SpineLazy.Value;

        public void Deconstruct(
            out Digit<T, V> left,
            out ILazy<FTree<Digit<T, V>, V>> spine,
            out Digit<T, V> right
        ) {
            left = Left;
            spine = SpineLazy;
            right = Right;
        }

        public override V Measure => _hasMeasure ? _measure 
            : ((_measure, _hasMeasure) = (V.Add(Left.Measure, Spine.Measure, Right.Measure), true))._measure;

        internal override View<T, V> toViewL() => new(Left.Head, deepL(Left.Tail, SpineLazy, Right));
        internal override View<T, V> toViewR() => new(Right.Last, deepR(Left, SpineLazy, Right.Init));

        public override TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) => 
            Left.ReduceRight(
                reduceOp,
                Spine.ReduceRight(
                    (a, b) => a.ReduceRight(reduceOp, b),
                    Right.ReduceRight(reduceOp, other)
                )
            );

        public override TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) =>
            Right.ReduceLeft(
                reduceOp,
                Spine.ReduceLeft(
                    (a, b) => b.ReduceLeft(reduceOp, a),
                    Left.ReduceLeft(reduceOp, other)
                )
            );

        public override FTree<T, V> Prepend(T toAdd) {
            if (Left.Length == 4)
                return new Deep(
                    new Digit<T, V>(toAdd, Left[0]),
                    Lazy.From((m, l) => m.Prepend(new Digit<T, V>(l[1], l[2], l[3])), Spine, Left),
                    Right
                );
            return new Deep(Left.Prepend(toAdd), SpineLazy, Right);
        }

        public override FTree<T, V> Append(T toAdd)
        {
            if (Right.Length == 4)
                return new Deep(
                    Left,
                    Lazy.From((m, r) => m.Append(new Digit<T, V>(r[0], r[1], r[2])), Spine, Right),
                    new Digit<T, V>(Right[3], toAdd)
                );
            return new Deep(Left, SpineLazy, Right.Append(toAdd));
        }

        public override ref readonly T Head => ref Left.Head;
        public override ref readonly T Last => ref Right.Last;

        public override (FTree<T, V>, T, FTree<T, V>) SplitTree(Func<V, bool> p, V i) {
            var vpr = V.Add(i, Left.Measure);
            if (p(vpr)) {
                var (l, x, r) = splitDigit(p, i, Left);
                return (createRangeOptimized(l), x, deepL(r, SpineLazy, Right));
            }
            var spine = Spine;
            var vm = V.Add(vpr, spine.Measure);
            if (p(vm)) {
                var (ml, xs, mr) = spine.SplitTree(p, vpr);
                var (l, x, r) = splitDigit(p, V.Add(vpr, ml.Measure), xs);
                return (deepR(Left, ml, l), x, deepL(r, mr, Right));
            }
            else {
                var (l, x, r) = splitDigit(p, vm, Right);
                return (deepR(Left, spine, l), x, createRangeOptimized(r));
            }
        }

        public override (ILazy<FTree<T, V>>, T, ILazy<FTree<T, V>>) SplitTreeLazy(Func<V, bool> p, V i) {
            // TODO: eliminate unnecessary .ToArray()?
            var vpr = V.Add(i, Left.Measure);
            if (p(vpr)) {
                var (l, x, r) = splitDigit(p, i, Left);
                // top level digits have up to 4 elements, no need for lazy alloc
                return (
                    Lazy.From(l => createRangeOptimized(l), l.ToArray()), 
                    x, 
                    Lazy.From((r, m, sf) => deepL(r, m, sf), r.ToArray(), SpineLazy, Right)
                );
            }
            var spine = Spine;
            var vm = V.Add(vpr, spine.Measure);
            if (p(vm)) {
                var (ml, xs, mr) = spine.SplitTreeLazy(p, vpr);
                var (l, x, r) = splitDigit(p, V.Add(vpr, ml.Value.Measure), xs);
                return (
                    Lazy.From((pr, ml, l) => deepR(pr, ml, l), Left, ml, l.ToArray()), 
                    x, 
                    Lazy.From((r, mr, sf) => deepL(r, mr, sf), r.ToArray(), mr, Right)
                );
            } else {
                var (l, x, r) = splitDigit(p, vm, Right);
                return (
                    Lazy.From((pr, m, l) => deepR(pr, m, l), Left, spine, l.ToArray()), 
                    x, 
                    Lazy.From(r => createRangeOptimized(r), r.ToArray())
                );
            }
        }

        public override IEnumerator<T> GetEnumerator() {
            foreach (var elem in Left) 
                yield return elem;
            foreach (var node in Spine)
            foreach (var nodeValue in node) 
                yield return nodeValue;
            foreach (var elem in Right) 
                yield return elem;
        }

        public override IEnumerator<T> GetReverseEnumerator() {
            for (var i = Right.Length - 1; i >= 0; --i)
                yield return Right[i];
            var it = Spine.GetReverseEnumerator();
            while (it.MoveNext())
            {
                var node = it.Current;
                foreach (var nodeElem in node!)
                    yield return nodeElem;
            }

            for (var i = Left.Length - 1; i >= 0; --i)
                yield return Left[i];
        }
    }
}