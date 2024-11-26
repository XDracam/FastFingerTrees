using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DracTec.FTrees.Impl;

[InlineArray(4)] 
internal struct Buffer<T> { private T value; }

// We are reusing digits as nodes. Node-like digits should have 2 or 3 values.
// A digit can have between 1 and 4 values, and even none in the case of Concat.
// This is not validated for performance reasons.
internal sealed class Digit<T, V> : IFTreeElement<V>
where T : IFTreeElement<V> where V : struct, IFTreeMeasure<V>
{
    private Buffer<T> values; // between 1 and 4 values
    private readonly int length;
    
    private bool _hasMeasure;
    private V _measure; // could be lazy, but that adds overhead on average

    public V Measure => _hasMeasure ? _measure : ((_hasMeasure, _measure) = (true, V.Add(Values)))._measure;

    private Span<T> valuesSpan => values[..length];
    public ReadOnlySpan<T> Values => values[..length];

    public Digit() {
        values = new();
        _hasMeasure = true;
    }

    public Digit(params ReadOnlySpan<T> values) {
        length = values.Length;
        values.CopyTo(valuesSpan);
    }
    
    public ref readonly T Head => ref values[0];
    public ReadOnlySpan<T> Tail => values[1..length];
    
    public ref readonly T Last => ref values[length-1];
    public ReadOnlySpan<T> Init => values[..(length-1)];

    public ref readonly T this[int idx] => ref values[idx];

    // Allow use in `foreach`, but we do not implement IEnumerable<T> to disallow slow LINQ on these
    public Enumerator GetEnumerator() => new(this);
    public struct Enumerator(Digit<T, V> digit)
    {
        private int idx = -1;

        public bool MoveNext() {
            idx += 1;
            return idx < digit.length;
        }

        public T Current => digit[idx];
    }

    public TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) {
        var acc = other;
        for (var i = length - 1; i >= 0; --i) 
            acc = reduceOp(values[i], acc);
        return acc;
    }

    public TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) {
        var acc = other;
        for (var i = 0; i < length; ++i) 
            acc = reduceOp(acc, values[i]);
        return acc;
    }

    public Digit<T, V> Prepend(T value) => length switch {
        1 => new(value, values[0]),
        2 => new(value, values[0], values[1]),
        3 => new(value, values[0], values[1], values[2]),
        _ => throw new InvalidOperationException()
    };

    public Digit<T, V> Append(T value) => length switch {
        1 => new(values[0], value),
        2 => new(values[0], values[1], value),
        3 => new(values[0], values[1], values[2], value),
        _ => throw new InvalidOperationException()
    };
}