using System;
using System.Collections.Immutable;

namespace DracTec.FTrees.Impl;

internal sealed class Node<T, V> : IFTreeElement<V> where T : IFTreeElement<V> where V : struct, IFTreeMeasure<V>
{
    public readonly ImmutableArray<T> Values;

    private bool _hasMeasure;
    private V _measure;
    
    public V Measure => _hasMeasure ? _measure : ((_hasMeasure, _measure) = (true, V.Add(Values.AsSpan())))._measure;

    public Node() => throw new InvalidOperationException();
    public Node(T first) => throw new InvalidOperationException();
    
    public Node(ReadOnlySpan<T> values) => Values = [..values];
    public Node(params ImmutableArray<T> values) => Values = values;

    public TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) {
        var start = Values.Length == 3 ? reduceOp(Values[2], other) : other;
        return reduceOp(Values[0], reduceOp(Values[1], start));
    }

    public TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) {
        var start = Values.Length == 3 ? reduceOp(other, Values[2]) : other;
        return reduceOp(reduceOp(start, Values[1]), Values[0]);
    }

    public Digit<T, V> ToDigit() => _hasMeasure ? new(Values, _measure) : new(Values);
}