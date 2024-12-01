using System;
using System.Collections.Generic;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

using static FTreeImplUtils;

public abstract partial class FTree<T, V>
{
    internal sealed class Deep(
        Digit<T, V> left,
        ILazy<FTree<Digit<T, V>, V>> spine,
        Digit<T, V> right
    ) : FTree<T, V>
    {
        private V _measure;
        private bool _hasMeasure;
        
        public readonly Digit<T, V> Left = left;
        public readonly Digit<T, V> Right = right;
        public readonly ILazy<FTree<Digit<T, V>, V>> Spine = spine;

        public void Deconstruct(
            out Digit<T, V> left,
            out ILazy<FTree<Digit<T, V>, V>> outSpine,
            out Digit<T, V> right
        ) {
            left = Left;
            outSpine = Spine;
            right = Right;
        }

        public override V Measure => _hasMeasure ? _measure 
            : ((_measure, _hasMeasure) = (V.Add(Left.Measure, Spine.Value.Measure, Right.Measure), true))._measure;

        internal override View<T, V> toViewL() => new(Left.Head, deepL(Left.Tail, Spine, Right));
        internal override View<T, V> toViewR() => new(Right.Last, deepR(Left, Spine, Right.Init));

        public override TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) => 
            Left.ReduceRight(
                reduceOp,
                Spine.Value.ReduceRight(
                    (a, b) => a.ReduceRight(reduceOp, b),
                    Right.ReduceRight(reduceOp, other)
                )
            );

        public override TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) =>
            Right.ReduceLeft(
                reduceOp,
                Spine.Value.ReduceLeft(
                    (a, b) => b.ReduceLeft(reduceOp, a),
                    Left.ReduceLeft(reduceOp, other)
                )
            );

        public override FTree<T, V> Prepend(T toAdd) {
            if (Left.Length == 4)
                return new Deep(
                    new Digit<T, V>(toAdd, Left[0]),
                    Lazy.From((m, l) => m.Value.Prepend(new Digit<T, V>(l[1], l[2], l[3])), Spine, Left),
                    Right
                );
            return new Deep(Left.Prepend(toAdd), Spine, Right);
        }

        public override FTree<T, V> Append(T toAdd)
        {
            if (Right.Length == 4)
                return new Deep(
                    Left,
                    Lazy.From((m, r) => m.Value.Append(new Digit<T, V>(r[0], r[1], r[2])), Spine, Right),
                    new Digit<T, V>(Right[3], toAdd)
                );
            return new Deep(Left, Spine, Right.Append(toAdd));
        }

        public override ref readonly T Head => ref Left.Head;
        public override ref readonly T Last => ref Right.Last;

        public override (FTree<T, V>, T, FTree<T, V>) SplitTree(Func<V, bool> p, V i) {
            var vpr = V.Add(i, Left.Measure);
            if (p(vpr)) {
                var (l, x, r) = splitDigit(p, i, Left);
                return (createRangeOptimized(l), x, deepL(r, Spine, Right));
            }
            var vm = V.Add(vpr, Spine.Value.Measure);
            if (p(vm)) {
                var (ml, xs, mr) = Spine.Value.SplitTree(p, vpr);
                var (l, x, r) = splitDigit(p, V.Add(vpr, ml.Measure), xs);
                return (deepR(Left, Lazy.From(ml), l), x, deepL(r, Lazy.From(mr), Right));
            }
            else {
                var (l, x, r) = splitDigit(p, vm, Right);
                return (deepR(Left, Spine, l), x, createRangeOptimized(r));
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
                    Lazy.From((r, m, sf) => deepL(r, m, sf), r.ToArray(), Spine, Right)
                );
            }
            var spine = Spine.Value;
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
                    Lazy.From((pr, m, l) => deepR(pr, m, l), Left, Spine, l.ToArray()), 
                    x, 
                    Lazy.From(r => createRangeOptimized(r), r.ToArray())
                );
            }
        }

        public override IEnumerator<T> GetEnumerator() {
            foreach (var elem in Left) 
                yield return elem;
            foreach (var node in Spine.Value)
            foreach (var nodeValue in node) 
                yield return nodeValue;
            foreach (var elem in Right) 
                yield return elem;
        }

        public override IEnumerator<T> GetReverseEnumerator() {
            for (var i = Right.Length - 1; i >= 0; --i)
                yield return Right[i];
            var it = Spine.Value.GetReverseEnumerator();
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