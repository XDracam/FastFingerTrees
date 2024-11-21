using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace FTrees.Benchmarks;

public class Contains
{
    [Params(
        // 100,
        // 1000,
        // 10000,
        // 30000
        Constants.ParamsSize
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
    public void ImmutableOrderedSetMerge() {
        foreach (var i in testArray)
            _ = testOrderedSet.Contains(i);
    }

    [Benchmark]
    public void ImmutableHashSetMerge() {
        foreach (var i in testArray)
            _ = testHashSet.Contains(i);
    }
        
    [Benchmark]
    public void ImmutableSortedSetMerge() {
        foreach (var i in testArray)
            _ = testSortedSet.Contains(i);
    }
        
    [Benchmark]
    public void ImmutableArrayMerge() {
        foreach (var i in testArray)
            _ = testArray.Contains(i);
    }
}