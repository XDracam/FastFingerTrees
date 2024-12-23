﻿using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace DracTec.FTrees.Benchmarks.ImmutableSeq;

public class Add {

    [Params(
        // 100,
        // 1000,
        // 10000,
        // 30000
        _Utils.ParamsSize
    )]
    public int Count;

    [GlobalSetup]
    public void Setup() { }

    [Benchmark]
    public void ListAdd() {
        var list = new List<int>();
        for (var i = 0; i < Count; ++i)
            list.Add(i);
        _Utils.Use(list);
    }
        
    [Benchmark]
    public void ImmutableArrayAdd()
    {
        var list = ImmutableArray<int>.Empty;
        for (var i = 0; i < Count; ++i)
            list = list.Add(i);
        _Utils.Use(list);
    }
        
    [Benchmark]
    public void ImmutableListAdd()
    {
        var list = ImmutableList<int>.Empty;
        for (var i = 0; i < Count; ++i)
            list = list.Add(i);
        _Utils.Use(list);
    }
        
    [Benchmark]
    public void ImmutableSeqAdd()
    {
        var list = ImmutableSeq<int>.Empty;
        for (var i = 0; i < Count; ++i)
            list = list.Add(i);
        _Utils.Use(list);
    }
}