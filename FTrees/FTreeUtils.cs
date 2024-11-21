using System;

namespace DracTec.FTrees;

// Utils that do not depend on T and V in the FTree itself
internal static class FTreeUtils 
{
    // for Concat
    // PREPEND <|' -> reduceR
    // APPEND |>' -> reduceL
    internal static FTree<A, V> app3<A, V>(FTree<A, V> self, Digit<A, V> ts, FTree<A, V> other)
    where A : IMeasured<V> where V : struct, IMeasure<V> {
        return (self, other) switch {
            (FTree<A, V>.EmptyT, var xs) => prependDigit(ts, xs),
            (var xs, FTree<A, V>.EmptyT) => appendDigit(ts, xs),
            (FTree<A, V>.Single(var x), var xs) => prependDigit(ts, xs).Prepend(x),
            (var xs, FTree<A, V>.Single(var x)) => appendDigit(ts, xs).Append(x),
            (FTree<A, V>.Deep(var pr1, var m1, var sf1), FTree<A, V>.Deep(var pr2, var m2, var sf2)) => 
                new FTree<A, V>.Deep(
                    pr1,
                    new(() => app3(
                        m1.Value,
                        new Digit<Node<A, V>, V>(nodes<A, V>(concat(sf1.Values, ts.Values, pr2.Values))), 
                        m2.Value
                    )),
                    sf2
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

        static A[] concat(A[] first, A[] second, A[] third) {
            // compute everything only once!
            var al = first.Length;
            var bl = second.Length;
            var cl = third.Length;
            var abl = al + bl;
            
            var res = new A[abl + cl];
            Array.Copy(first, 0, res, 0, al);
            Array.Copy(second, 0, res, al, bl);
            Array.Copy(third, 0, res, abl, cl);
            return res;
        }
    }
    
    // optimized to avoid unnecessary allocations
    internal static Node<A, V>[] nodes<A, V>(ReadOnlySpan<A> arr) 
    where A : IMeasured<V> where V : struct, IMeasure<V> {
        var length = arr.Length;
        var mod = length % 3;
        var res = new Node<A, V>[(length / 3) + (mod > 0 ? 1 : 0)];

        // 0 -> no node2s, 2 -> 1 node2, 1 -> 2 node2s (all nodes need 2 or 3 items)
        var numNode2 = (mod * 2) % 3; 
        var node3EndIdx = length - numNode2 * 2;
                
        var arrIdx = 0;
        var resIdx = 0;
                
        while (arrIdx < node3EndIdx)
            res[resIdx++] = new Node<A, V>(arr[arrIdx++], arr[arrIdx++], arr[arrIdx++]);
            
        for (var i = 0; i < numNode2; ++i)
            res[resIdx++] = new Node<A, V>(arr[arrIdx++], arr[arrIdx++]);
            
        return res;
    }
    
    // digits have at most 4 items, so O(1)
    internal static Triple<A> splitDigit<A, V>(Func<V, bool> p, V i, Digit<A, V> digit) 
    where A : IMeasured<V> where V : struct, IMeasure<V> {
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
}

// Value tuples aren't ref structs, so this approach is faster
internal readonly ref struct Triple<T>
{
    public readonly ReadOnlySpan<T> Item1;
    public readonly T Item2;
    public readonly ReadOnlySpan<T> Item3;

    public Triple(ReadOnlySpan<T> item1, T item2, ReadOnlySpan<T> item3) {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
    }

    public void Deconstruct(out ReadOnlySpan<T> first, out T second, out ReadOnlySpan<T> third) {
        first = Item1;
        second = Item2;
        third = Item3;
    }
}