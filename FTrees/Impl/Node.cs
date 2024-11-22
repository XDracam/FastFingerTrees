using System;

namespace DracTec.FTrees.Impl;

internal sealed class Node<T, V> : IFTreeElement<V> where T : IFTreeElement<V> where V : struct, IFTreeMeasure<V>
{
    public readonly bool HasThird;
    public readonly T First, Second, Third;

    private bool _hasMeasure;
    private V _measure;
    
    public V Measure {
        get {
            if (_hasMeasure) return _measure;
            _measure = HasThird
                ? V.Add(First.Measure, Second.Measure, Third.Measure)
                : V.Add(First.Measure, Second.Measure);
            _hasMeasure = true;
            return _measure;
        }
    }

    public Node(T first, T second) {
        HasThird = false;
        First = first;
        Second = second;
        Third = default;
    }
    
    public Node(T first, T second, T third) {
        HasThird = true;
        First = first;
        Second = second;
        Third = third;
    }

    public TRes ReduceRight<TRes>(Func<T, TRes, TRes> reduceOp, TRes other) {
        var start = HasThird ? reduceOp(Third, other) : other;
        return reduceOp(First, reduceOp(Second, start));
    }

    public TRes ReduceLeft<TRes>(Func<TRes, T, TRes> reduceOp, TRes other) {
        var start = HasThird ? reduceOp(other, Third) : other;
        return reduceOp(reduceOp(start, Second), First);
    }

    public Digit<T, V> ToDigit() => HasThird ? new Digit<T, V>(First, Second, Third) : new Digit<T, V>(First, Second);
}