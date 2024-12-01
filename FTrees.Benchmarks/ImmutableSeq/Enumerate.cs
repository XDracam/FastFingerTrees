using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace DracTec.FTrees.Benchmarks.ImmutableSeq;

public class Enumerate {
        
    private List<int> list;
    private ImmutableArray<int> immutableArray;
    private ImmutableList<int> immutableList;
    private ImmutableSeq<int> immutableSeq;

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
        var range = Enumerable.Range(0, Count).ToArray();
        list = new List<int>(range);
        immutableArray = ImmutableArray.CreateRange(range);
        immutableList = ImmutableList.CreateRange(range);
        immutableSeq = FTrees.ImmutableSeq.CreateRange(range);

    }
        
    [Benchmark]
    public void ListEnumerate()
    {
        foreach (var item in list) { _Utils.Use(item); }
    }
        
    [Benchmark]
    public void ImmutableArrayEnumerate()
    {
        foreach (var item in immutableArray) { _Utils.Use(item); }
    }
        
    [Benchmark]
    public void ImmutableListEnumerate()
    {
        foreach (var item in immutableList) { _Utils.Use(item); }
    }
        
    [Benchmark]
    public void ImmutableSeqEnumerate()
    {
        foreach (var item in immutableSeq) { _Utils.Use(item); }
    }
}