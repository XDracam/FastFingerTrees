﻿using System;

namespace DracTec.FTrees.Impl;

// Utils that depend on different generics than the FTree itself.
// Moved out to reduce the strain on the JIT compiler.
internal static class FTreeImplUtils 
{
    // ReSharper disable VariableHidesOuterVariable
    
    internal static View<A, V> toViewL<A, V>(FTree<A, V> self) where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        return self switch {
            FTree<A, V>.EmptyT => new View<A, V>(),
            FTree<A, V>.Single(var x) => new View<A, V>(x, FTree<A, V>.EmptyT.Instance),
            FTree<A, V>.Deep(var pr, var m, var sf) => new View<A, V>(pr.Head, deepL(pr.Tail, m, sf)),
            _ => throw new InvalidOperationException()
        };
    }
    
    internal static FTree<A, V> deepL<A, V>(
        ReadOnlySpan<A> pr, 
        ILazy<FTree<Digit<A, V>, V>> m, 
        Digit<A, V> sf
    ) where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        if (pr.Length > 0) 
            return new FTree<A, V>.Deep(new(pr), m, sf);
        
        return m.Value switch {
            FTree<Digit<A, V>, V>.EmptyT => 
                FTree<A, V>.createRangeOptimized(sf.Values),
            FTree<Digit<A, V>, V>.Single(var x) => 
                new FTree<A, V>.Deep(x, Lazy.From(FTree<Digit<A, V>, V>.Empty), sf),
            FTree<Digit<A, V>, V>.Deep d => 
                new FTree<A, V>.Deep(d.Left.Head, Lazy.From(d => deepL(d.Left.Tail, d.Spine, d.Right), d), sf),
            _ => throw new InvalidOperationException()
        };
    }
    
    
    internal static View<A, V> toViewR<A, V>(FTree<A, V> self) where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        return self switch {
            FTree<A, V>.EmptyT => new View<A, V>(),
            FTree<A, V>.Single(var x) => new View<A, V>(x, FTree<A, V>.EmptyT.Instance),
            FTree<A, V>.Deep(var pr, var m, var sf) => new View<A, V>(sf.Last, deepR(pr, m, sf.Init)),
            _ => throw new InvalidOperationException()
        };
    }
    
    internal static FTree<A, V> deepR<A, V>(
        Digit<A, V> pr,
        ILazy<FTree<Digit<A, V>, V>> m, 
        ReadOnlySpan<A> sf
    ) where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        if (sf.Length > 0) 
            return new FTree<A, V>.Deep(pr, m, new(sf));
        
        return m.Value switch {
            FTree<Digit<A, V>, V>.EmptyT => 
                FTree<A, V>.createRangeOptimized(pr.Values),
            FTree<Digit<A, V>, V>.Single(var x) => 
                new FTree<A, V>.Deep(pr, Lazy.From(FTree<Digit<A, V>, V>.Empty), x),
            FTree<Digit<A, V>, V>.Deep d => 
                new FTree<A, V>.Deep(pr, Lazy.From(d => deepR(d.Left, d.Spine, d.Right.Init), d), d.Right.Last),
            _ => throw new InvalidOperationException()
        };
    }
    
    internal static FTree<A, V> app2<A, V>(FTree<A, V> self, FTree<A, V> other)
    where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        return (self, other) switch {
            (FTree<A, V>.EmptyT, var xs) => xs,
            (var xs, FTree<A, V>.EmptyT) => xs,
            (FTree<A, V>.Single(var x), var xs) => xs.Prepend(x),
            (var xs, FTree<A, V>.Single(var x)) => xs.Append(x),
            (FTree<A, V>.Deep d1, FTree<A, V>.Deep d2) => 
                new FTree<A, V>.Deep(
                    d1.Left,
                    Lazy.From((d1, d2) => app3(
                        d1.Spine.Value,
                        new Digit<Digit<A, V>, V>(
                            // TODO: this allocates log n arrays - can we get rid of that?
                            nodes<A, V>(concat(d1.Right.Values, ReadOnlySpan<A>.Empty, d2.Left.Values))), 
                        d2.Spine.Value
                    ), d1, d2),
                    d2.Right
                ),
            _ => throw new InvalidOperationException()
        };
    }
    
    // for Concat
    // PREPEND <|' -> reduceR
    // APPEND |>' -> reduceL
    internal static FTree<A, V> app3<A, V>(FTree<A, V> self, Digit<A, V> ts, FTree<A, V> other)
    where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        return (self, other) switch {
            (FTree<A, V>.EmptyT, var xs) => prependDigit(ts, xs),
            (var xs, FTree<A, V>.EmptyT) => appendDigit(ts, xs),
            (FTree<A, V>.Single(var x), var xs) => prependDigit(ts, xs).Prepend(x),
            (var xs, FTree<A, V>.Single(var x)) => appendDigit(ts, xs).Append(x),
            (FTree<A, V>.Deep d1, FTree<A, V>.Deep d2) => 
                new FTree<A, V>.Deep(
                    d1.Left,
                    Lazy.From((d1, d2, ts) => app3(
                        d1.Spine.Value,
                        new Digit<Digit<A, V>, V>(nodes<A, V>(concat(d1.Right.Values, ts.Values, d2.Left.Values))), 
                        d2.Spine.Value
                    ), d1, d2, ts),
                    d2.Right
                ),
            _ => throw new InvalidOperationException()
        };
        
        static FTree<A, V> prependDigit(Digit<A, V> digit, FTree<A, V> tree) {
            for (var i = digit.Values.Length - 1; i >= 0; --i) 
                tree = tree.Prepend(digit.Values[i]);
            return tree;
        }
    
        static FTree<A, V> appendDigit(Digit<A, V> digit, FTree<A, V> tree) {
            for (var i = 0; i < digit.Values.Length; ++i) 
                tree = tree.Append(digit.Values[i]);
            return tree;
        }
    }
    
     private static A[] concat<A>(ReadOnlySpan<A> first, ReadOnlySpan<A> second, ReadOnlySpan<A> third) {
        // compute everything only once!
        var al = first.Length;
        var bl = second.Length;
        var cl = third.Length;
        var abl = al + bl;
            
        var res = new A[abl + cl];
        var span = res.AsSpan();
            
        first.CopyTo(span[..al]);
        second.CopyTo(span[al..abl]);
        third.CopyTo(span[abl..]);
            
        return res;
    }
    
    // optimized to avoid unnecessary allocations
    internal static Digit<A, V>[] nodes<A, V>(ReadOnlySpan<A> arr) 
    where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        var length = arr.Length;
        var mod = length % 3;
        var res = new Digit<A, V>[(length / 3) + (mod > 0 ? 1 : 0)];

        // 0 -> no node2s, 2 -> 1 node2, 1 -> 2 node2s (all nodes need 2 or 3 items)
        var numDigit2 = (mod * 2) % 3; 
        var node3EndIdx = length - numDigit2 * 2;
                
        var arrIdx = 0;
        var resIdx = 0;
                
        while (arrIdx < node3EndIdx)
            res[resIdx++] = new Digit<A, V>(arr[arrIdx++], arr[arrIdx++], arr[arrIdx++]);
            
        for (var i = 0; i < numDigit2; ++i)
            res[resIdx++] = new Digit<A, V>(arr[arrIdx++], arr[arrIdx++]);
            
        return res;
    }
    
    // digits have at most 4 items, so O(1)
    internal static Triple<A> splitDigit<A, V>(Func<V, bool> p, V i, Digit<A, V> digit) 
    where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        if (digit.Values.Length == 1)
            return new([], digit.Head, []);
        for (var idx = 0; idx < digit.Values.Length; ++idx) {
            i = V.Add(i, digit.Values[idx].Measure);
            if (p(i)) return split(digit.Values, idx);
        }
        // not found anything, take last element
        return new(digit.Init, digit.Last, []);

        static Triple<A> split(ReadOnlySpan<A> array, int idx) {
            var first = array[..idx];
            var second = array[(idx+1)..];
            return new(first, array[idx], second);
        }
    }
    
    // Value tuples aren't ref structs, so this approach is faster
    internal readonly ref struct Triple<T>(ReadOnlySpan<T> item1, T item2, ReadOnlySpan<T> item3)
    {
        public readonly ReadOnlySpan<T> Item1 = item1;
        public readonly T Item2 = item2;
        public readonly ReadOnlySpan<T> Item3 = item3;

        public void Deconstruct(out ReadOnlySpan<T> first, out T second, out ReadOnlySpan<T> third) {
            first = Item1;
            second = Item2;
            third = Item3;
        }
    }
}



