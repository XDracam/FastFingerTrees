using System;

namespace DracTec.FTrees.Impl;

// Either a producer or an already calculated value.
// Is a class, because shared values should only calculate once.
// This cannot be done safely without a managed heap allocation.
// Also not a regular Lazy because we do not care about parallelization. 
// All computations in FTrees are pure, so calculating twice 
//  in a parallel environment won't hurt, as long as T is a reference type.
internal sealed class LazyThunk<T> where T : class
{
    private bool _hasValue;
    private Func<T> _producer;
    private T _value;

    public T Value { get {
        if (!_hasValue) {
            _value = _producer();
            _hasValue = true;
            _producer = null;
        }
        return _value;
    }}

    public LazyThunk(T value) {
        _value = value;
        _hasValue = true;
    }

    public LazyThunk(Func<T> getter) {
        _producer = getter;
    }
}