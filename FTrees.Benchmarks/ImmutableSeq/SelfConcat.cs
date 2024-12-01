using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace DracTec.FTrees.Benchmarks.ImmutableSeq;

public class SelfConcat {
        
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
        immutableArray = [..range];
        immutableList = [..range];
        immutableSeq = [..range];
    }
        
    [Benchmark]
    public void ListConcat() {
        _Utils.Use(list.Concat(list).ToList()); // "immutable concat"
    }
        
    [Benchmark]
    public void ImmutableArrayConcat() {
        _Utils.Use(immutableArray.AddRange(immutableArray));
    }
        
    [Benchmark]
    public void ImmutableListConcat() {
        _Utils.Use(immutableList.AddRange(immutableList));
    }
        
    [Benchmark]
    public void ImmutableSeqConcat()
    {
        _Utils.Use(immutableSeq.AddRange(immutableSeq));
    }
}