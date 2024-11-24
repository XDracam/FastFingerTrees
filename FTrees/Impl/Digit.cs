using System;
using System.Collections.Immutable;

namespace DracTec.FTrees.Impl;

// We are reusing digits as nodes. Node-like digits should have 2 or 3 values.
// A digit can have between 1 and 4 values, and even none in the case of Concat.
// This is not validated for performance reasons.
internal sealed class Digit<T, V> : IFTreeElement<V> where T : IFTreeElement<V> where V : struct, IFTreeMeasure<V>
{
    public readonly ImmutableArray<T> Values; // between 1 and 4 values
    
    private bool _hasMeasure;
    private V _measure; // could be lazy, but that adds overhead on average

    public V Measure => _hasMeasure ? _measure : ((_hasMeasure, _measure) = (true, V.Add(Values.AsSpan())))._measure;

    public Digit() {
        Values = ImmutableArray<T>.Empty;
        _hasMeasure = true;
    }
    
    public Digit(ReadOnlySpan<T> values) => Values = [..values];
    public Digit(params ImmutableArray<T> values) => Values = values;

    public TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) {
        var acc = other;
        for (var i = Values.Length - 1; i >= 0; --i) 
            acc = reduceOp(Values[i], acc);
        return acc;
    }

    public TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) {
        var acc = other;
        for (var i = 0; i < Values.Length; ++i) 
            acc = reduceOp(acc, Values[i]);
        return acc;
    }

    public Digit<T, V> Prepend(T value) => Values.Length switch {
        1 => new(value, Values[0]),
        2 => new(value, Values[0], Values[1]),
        3 => new(value, Values[0], Values[1], Values[2]),
        _ => throw new InvalidOperationException()
    };

    public Digit<T, V> Append(T value) => new(Values.Add(value));

    public ref readonly T Head => ref Values.AsSpan()[0];
    public ReadOnlySpan<T> Tail => Values.AsSpan()[1..];
    
    public ref readonly T Last => ref Values.AsSpan()[^1];
    public ReadOnlySpan<T> Init => Values.AsSpan()[..^1];
}