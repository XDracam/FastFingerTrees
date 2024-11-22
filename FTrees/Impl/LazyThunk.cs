using System;

namespace DracTec.FTrees.Impl;

// Either a producer or an already calculated value.
// The extra level of indirection adds performance when initialized eagerly.
// Use carefully! You don't want to copy this by accident, making the cached value useless.
internal struct LazyThunk<T>
{
    private bool _hasValue;
    private T _value;
    private Func<T> _producer;

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

// Either a producer or an already calculated value.
// The extra level of indirection adds performance when initialized eagerly.
// A struct is worse in some cases, e.g. when reusing the thunk in case of Deep.
internal sealed class LazyThunkClass<T>
{
    private bool _hasValue;
    private T _value;
    private Func<T> _producer;

    public T Value { get {
        if (!_hasValue) {
            _value = _producer();
            _hasValue = true;
            _producer = null;
        }
        return _value;
    }}

    public LazyThunkClass(T value) {
        _value = value;
        _hasValue = true;
    }

    public LazyThunkClass(Func<T> getter) {
        _producer = getter;
    }
}