using System;
using System.Collections.Immutable;

namespace DracTec.FTrees.Impl;

internal readonly struct Digit<T, V> where T : IFTreeElement<V> where V : struct, IFTreeMeasure<V>
{
    public readonly ImmutableArray<T> Values; // between 1 and 4 values
    private readonly V measure; // could be lazy, but that adds overhead on average

    public V Measure => measure;

    public Digit(params ReadOnlySpan<T> values) {
        Values = [..values];
        measure = V.Add(Values.AsSpan());
    }

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
    
    public Digit<T, V> Append(T value) => Values.Length switch {
        1 => new(Values[0], value),
        2 => new(Values[0], Values[1], value),
        3 => new(Values[0], Values[1], Values[2], value),
        _ => throw new InvalidOperationException()
    };

    public T Head => Values[0];
    public ReadOnlySpan<T> Tail => Values.AsSpan()[1..];
    
    public T Last => Values[^1];
    public ReadOnlySpan<T> Init => Values.AsSpan()[..^1];
}