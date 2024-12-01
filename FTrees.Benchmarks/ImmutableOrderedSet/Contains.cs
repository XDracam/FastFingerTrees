using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DracTec.FTrees.Benchmarks.ImmutableSeq;

namespace DracTec.FTrees.Benchmarks.ImmutableOrderedSet;

public class Contains
{
    [Params(
        // 100,
        // 1000,
        // 10000,
        // 30000
        _Utils.ParamsSize
    )]
    public int Count;

    private ImmutableOrderedSet<int> testOrderedSet;
    private ImmutableHashSet<int> testHashSet;
    private ImmutableSortedSet<int> testSortedSet;
    private ImmutableArray<int> testArray;
    
    [GlobalSetup]
    public void Setup() {
        testOrderedSet = [..Enumerable.Range(0, Count)];
        testHashSet = [..Enumerable.Range(0, Count)];
        testSortedSet = [..Enumerable.Range(0, Count)];
        testArray = [..Enumerable.Range(0, Count)];
    }

    [Benchmark]
    public void ImmutableOrderedSetContains() {
        foreach (var i in testArray)
            _Utils.Use(testOrderedSet.Contains(i));
    }

    [Benchmark]
    public void ImmutableHashSetContains() {
        foreach (var i in testArray)
            _Utils.Use(testHashSet.Contains(i));
    }
        
    [Benchmark]
    public void ImmutableSortedSetContains() {
        foreach (var i in testArray)
            _Utils.Use(testSortedSet.Contains(i));
    }
        
    [Benchmark]
    public void ImmutableArrayContains() {
        foreach (var i in testArray)
            _Utils.Use(testArray.Contains(i));
    }
}