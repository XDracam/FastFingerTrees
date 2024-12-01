using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace DracTec.FTrees.Benchmarks.ImmutableSeq;

public class CreateRange {
        
    private int[] testData;

    [Params(
        // 100,
        // 1000,
        // 10000,
        // 30000
        _Utils.ParamsSize
    )]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        testData = new int[Count];
        for (int i = 0; i < testData.Length; i++)
            testData[i] = i;
    }
        
    [Benchmark]
    public void ListCreateRange()
    {
        var list = new List<int>(testData);
        _Utils.Use(list);
    }
        
    [Benchmark]
    public void ImmutableArrayCreateRange()
    {
        var list = ImmutableArray.CreateRange(testData);
        _Utils.Use(list);
    }
        
    [Benchmark]
    public void ImmutableListCreateRange()
    {
        var list = ImmutableList.CreateRange(testData);
        _Utils.Use(list);
    }
        
    [Benchmark]
    public void ImmutableSeqCreateRange()
    {
        var list = FTrees.ImmutableSeq.CreateRange(testData);
        _Utils.Use(list);
    }
}