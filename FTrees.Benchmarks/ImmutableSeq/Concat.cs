using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace DracTec.FTrees.Benchmarks.ImmutableSeq;

public class Concat {
        
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
    public void ListConcat() {
        _Utils.Use(list.Concat(immutableArray).ToList()); // "immutable concat"
    }
        
    [Benchmark]
    public void ImmutableArrayConcat() {
        _Utils.Use(immutableArray.AddRange(immutableArray));
    }
        
    [Benchmark]
    public void ImmutableListConcat() {
        _Utils.Use(immutableList.AddRange(immutableArray));
    }
        
    [Benchmark]
    public void ImmutableSeqConcat() {
        _Utils.Use(immutableSeq.AddRange(immutableArray));
    }
}