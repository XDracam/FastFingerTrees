using System;
using System.Collections;
using System.Collections.Generic;
using DracTec.FTrees.Impl;

namespace DracTec.FTrees;

using static FTreeImplUtils;

public interface IFTreeMeasure<TSelf> where TSelf : struct, IFTreeMeasure<TSelf>
{
    static abstract TSelf Add(in TSelf a, in TSelf b);
    static abstract TSelf Add(in TSelf a, in TSelf b, in TSelf c);
    static abstract TSelf Add<T>(ReadOnlySpan<T> values) where T : IFTreeElement<TSelf>; // for digits
}

public interface IFTreeElement<V> where V : struct, IFTreeMeasure<V> { V Measure { get; } }

// ReSharper disable VariableHidesOuterVariable

/// <summary>
/// A functional representation of persistent sequences supporting access to the ends in amortized constant time,
///  and concatenation and splitting in time logarithmic in the size of the smaller piece.
/// </summary>
/// <remarks> 
/// Based on https://www.staff.city.ac.uk/~ross/papers/FingerTree.pdf
/// </remarks>
public abstract partial class FTree<T, V> : IEnumerable<T>, IFTreeElement<V>
where T : IFTreeElement<V> where V : struct, IFTreeMeasure<V>
{
    private FTree() { } // only allow nested types to inherit

    public static FTree<T, V> Empty => EmptyT.Instance;
    
    public abstract V Measure { get; }

    internal abstract View<T, V> toViewL();
    internal abstract View<T, V> toViewR();
    
    public abstract TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other);
    public abstract TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other);
    
    public abstract FTree<T, V> Prepend(T toAdd);
    public abstract FTree<T, V> Append(T toAdd);

    public static FTree<T, V> Create(params ReadOnlySpan<T> values) => 
        CreateRange(values);
    
    public static FTree<T, V> CreateRange(ReadOnlySpan<T> values) =>
        createRangeOptimized(values);
    
    internal static FTree<T, V> createRangeOptimized(ReadOnlySpan<T> array) {
        var length = array.Length;
        switch (length) {
            case 0:
                return EmptyT.Instance;
            case 1:
                return new Single(array[0]);
            case <= 8:
                // Create a digit directly if possible
                var firstDigitLength = length / 2;
                return new Deep(
                    new Digit<T, V>(array[..firstDigitLength]), 
                    FTree<Digit<T, V>, V>.EmptyT.LazyInstance, 
                    new Digit<T, V>(array[firstDigitLength..])
                );
            default:
                // This case only happens in CreateRange
                var leftDigit = new Digit<T, V>(array[..3]);
                var rightDigit = new Digit<T, V>(array[^3..]);

                var arrForDigits = array[3..^3].ToArray();
                // Note: node needs 2 or 3 elements
                return new Deep(
                    leftDigit, 
                    // TODO: this allocates log n arrays - can we get rid of that?
                    Lazy.From(arr => FTree<Digit<T, V>, V>.createRangeOptimized(nodes<T, V>(arr)), arrForDigits),
                    rightDigit
                );
        }
    }

    public bool IsEmpty => this is EmptyT;

    public abstract ref readonly T Head { get; } 
    public abstract ref readonly T Last { get; }

    public FTree<T, V> Tail => toViewL().Tail;
    public FTree<T, V> Init => toViewR().Tail;

    // in paper: |><| (wtf)
    public FTree<T, V> Concat(FTree<T, V> other) => app2(this, other);
    
    // guaranteed to be O(logn)
    public abstract (FTree<T, V>, T, FTree<T, V>) SplitTree(Func<V, bool> p, V i);
    
    // guaranteed to be O(logn)
    public abstract (ILazy<FTree<T, V>>, T, ILazy<FTree<T, V>>) SplitTreeLazy(Func<V, bool> p, V i);

    public (FTree<T, V>, FTree<T, V>) Split(Func<V, bool> p) {
        if (this is EmptyT) return (Empty, Empty);
        if (p(Measure)) {
            var (l, x, r) = SplitTree(p, new());
            return (l, r.Prepend(x));
        }
        return (this, Empty);
    }
    
    public (ILazy<FTree<T, V>>, ILazy<FTree<T, V>>) SplitLazy(Func<V, bool> p) {
        if (this is EmptyT) return (EmptyT.LazyInstance, EmptyT.LazyInstance);
        if (p(Measure)) {
            var (l, x, r) = SplitTreeLazy(p, new());
            return (l, Lazy.From((r, x) => r.Value.Prepend(x), r, x));
        }
        return (Lazy.From(this), EmptyT.LazyInstance);
    }

    public FTree<T, V> TakeUntil(Func<V, bool> p) => SplitLazy(p).Item1.Value;
    public FTree<T, V> DropUntil(Func<V, bool> p) => SplitLazy(p).Item2.Value;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public abstract IEnumerator<T> GetEnumerator();
    public abstract IEnumerator<T> GetReverseEnumerator();

    

    
}