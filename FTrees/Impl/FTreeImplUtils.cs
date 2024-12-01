using System;

namespace DracTec.FTrees.Impl;

// ReSharper disable VariableHidesOuterVariable

// Utils that depend on different generics than the FTree itself.
// Moved out to reduce the strain on the JIT compiler.
internal static class FTreeImplUtils 
{
    // two versions of deepL and deepR because sometimes we already had to evaluate the spine and sometimes we didn't
    
    internal static FTree<A, V> deepL<A, V>(
        ReadOnlySpan<A> pr, 
        FTree<Digit<A, V>, V> m, 
        Digit<A, V> sf
    ) where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        if (pr.Length > 0) 
            return new FTree<A, V>.Deep(new(pr), Lazy.From(m), sf);
        
        return m switch {
            FTree<Digit<A, V>, V>.EmptyT => 
                FTree<A, V>.createRangeOptimized(sf.Values),
            FTree<Digit<A, V>, V>.Single(var x) => 
                new FTree<A, V>.Deep(x, FTree<Digit<A, V>, V>.EmptyT.LazyInstance, sf),
            FTree<Digit<A, V>, V>.Deep d => 
                new FTree<A, V>.Deep(d.Left.Head, Lazy.From(d => deepL(d.Left.Tail, d.SpineLazy, d.Right), d), sf),
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
                new FTree<A, V>.Deep(x, FTree<Digit<A, V>, V>.EmptyT.LazyInstance, sf),
            FTree<Digit<A, V>, V>.Deep d => 
                new FTree<A, V>.Deep(d.Left.Head, Lazy.From(d => deepL(d.Left.Tail, d.SpineLazy, d.Right), d), sf),
            _ => throw new InvalidOperationException()
        };
    }
    
    internal static FTree<A, V> deepR<A, V>(
        Digit<A, V> pr,
        FTree<Digit<A, V>, V> m, 
        ReadOnlySpan<A> sf
    ) where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        if (sf.Length > 0) 
            return new FTree<A, V>.Deep(pr, Lazy.From(m), new(sf));
        
        return m switch {
            FTree<Digit<A, V>, V>.EmptyT => 
                FTree<A, V>.createRangeOptimized(pr.Values),
            FTree<Digit<A, V>, V>.Single(var x) => 
                new FTree<A, V>.Deep(pr, FTree<Digit<A, V>, V>.EmptyT.LazyInstance, x),
            FTree<Digit<A, V>, V>.Deep d => 
                new FTree<A, V>.Deep(pr, Lazy.From(d => deepR(d.Left, d.SpineLazy, d.Right.Init), d), d.Right.Last),
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
                new FTree<A, V>.Deep(pr, FTree<Digit<A, V>, V>.EmptyT.LazyInstance, x),
            FTree<Digit<A, V>, V>.Deep d => 
                new FTree<A, V>.Deep(pr, Lazy.From(d => deepR(d.Left, d.SpineLazy, d.Right.Init), d), d.Right.Last),
            _ => throw new InvalidOperationException()
        };
    }
    
    internal static FTree<A, V> app2<A, V>(FTree<A, V> self, FTree<A, V> other)
    where A : IFTreeElement<V> where V : struct, IFTreeMeasure<V> {
        return (self, other) switch {
            (FTree<A, V>.EmptyT, var xs) => xs,
            (var xs, FTree<A, V>.EmptyT) => xs,
            (FTree<A, V>.Single s, var xs) => xs.Prepend(s.Value),
            (var xs, FTree<A, V>.Single s) => xs.Append(s.Value),
            (FTree<A, V>.Deep d1, FTree<A, V>.Deep d2) => 
                new FTree<A, V>.Deep(
                    d1.Left,
                    Lazy.From((d1, d2) => app3(
                        d1.Spine,
                        new Digit<Digit<A, V>, V>(
                            // TODO: this allocates log n arrays - can we get rid of that?
                            nodes<A, V>(concat(d1.Right.Values, ReadOnlySpan<A>.Empty, d2.Left.Values))), 
                        d2.Spine
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
            (FTree<A, V>.Single s, var xs) => prependDigit(ts, xs).Prepend(s.Value),
            (var xs, FTree<A, V>.Single s) => appendDigit(ts, xs).Append(s.Value),
            (FTree<A, V>.Deep d1, FTree<A, V>.Deep d2) => 
                new FTree<A, V>.Deep(
                    d1.Left,
                    Lazy.From((d1, d2, ts) => app3(
                        d1.Spine,
                        new Digit<Digit<A, V>, V>(nodes<A, V>(concat(d1.Right.Values, ts.Values, d2.Left.Values))), 
                        d2.Spine
                    ), d1, d2, ts),
                    d2.Right
                ),
            _ => throw new InvalidOperationException()
        };
        
        static FTree<A, V> prependDigit(Digit<A, V> digit, FTree<A, V> tree) {
            for (var i = digit.Length - 1; i >= 0; --i) 
                tree = tree.Prepend(digit[i]);
            return tree;
        }
    
        static FTree<A, V> appendDigit(Digit<A, V> digit, FTree<A, V> tree) {
            for (var i = 0; i < digit.Length; ++i) 
                tree = tree.Append(digit[i]);
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
        if (digit.Length == 1)
            return new([], digit.Head, []);
        for (var idx = 0; idx < digit.Length; ++idx) {
            i = V.Add(i, digit[idx].Measure);
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



