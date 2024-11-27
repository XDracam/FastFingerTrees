using System;

namespace DracTec.FTrees.Impl;

internal static class Lazy
{
    public static ILazy<T> From<T>(T value) where T : class => 
        new LazyThunk<T>(value);
    
    public static ILazy<T> From<T>(Func<T> producer) where T : class => 
        new LazyThunk<T>(producer);

    public static ILazy<T> From<T, Arg1>(Func<Arg1, T> producer, Arg1 arg1) where T : class => 
        new LazyThunk<T, Arg1>(producer, arg1);
    
    public static ILazy<T> From<T, Arg1, Arg2>(Func<Arg1, Arg2, T> producer, Arg1 arg1, Arg2 arg2) where T : class => 
        new LazyThunk<T, Arg1, Arg2>(producer, arg1, arg2);
    
    public static ILazy<T> From<T, Arg1, Arg2, Arg3>(
        Func<Arg1, Arg2, Arg3, T> producer, Arg1 arg1, Arg2 arg2, Arg3 arg3
    ) where T : class => 
        new LazyThunk<T, Arg1, Arg2, Arg3>(producer, arg1, arg2, arg3);
}

internal interface ILazy<out T> where T : class
{
    T Value { get; }
}

// Either a producer or an already calculated value.
// Is a class, because shared values should only calculate once.
// This cannot be done safely without a managed heap allocation.
// Also not a regular Lazy because we do not care about parallelization. 
// All computations in FTrees are pure, so calculating twice 
//  in a parallel environment won't hurt, as long as T is a reference type.
internal sealed class LazyThunk<T> : ILazy<T> where T : class
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

// Variant that avoids closure allocation with one extra argument
internal sealed class LazyThunk<T, Arg1>(Func<Arg1, T> getter, Arg1 arg1) : ILazy<T>
    where T : class
{
    private bool _hasValue;
    private Func<Arg1, T> _producer = getter;
    private T _value;

    public T Value { get {
        if (!_hasValue) {
            _value = _producer(arg1);
            _hasValue = true;
            _producer = null;
        }
        return _value;
    }}
}

// Variant that avoids closure allocation with two extra arguments
internal sealed class LazyThunk<T, Arg1, Arg2>(Func<Arg1, Arg2, T> getter, Arg1 arg1, Arg2 arg2) : ILazy<T>
    where T : class
{
    private bool _hasValue;
    private Func<Arg1, Arg2, T> _producer = getter;
    private T _value;

    public T Value { get {
        if (!_hasValue) {
            _value = _producer(arg1, arg2);
            _hasValue = true;
            _producer = null;
        }
        return _value;
    }}
}

// Variant that avoids closure allocation with two extra arguments
internal sealed class LazyThunk<T, Arg1, Arg2, Arg3>(
    Func<Arg1, Arg2, Arg3, T> getter, Arg1 arg1, Arg2 arg2, Arg3 arg3
) : ILazy<T> where T : class
{
    private bool _hasValue;
    private Func<Arg1, Arg2, Arg3, T> _producer = getter;
    private T _value;

    public T Value { get {
        if (!_hasValue) {
            _value = _producer(arg1, arg2, arg3);
            _hasValue = true;
            _producer = null;
        }
        return _value;
    }}
}