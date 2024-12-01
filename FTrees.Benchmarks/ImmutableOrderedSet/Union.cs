using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DracTec.FTrees.Benchmarks.ImmutableSeq;

namespace DracTec.FTrees.Benchmarks.ImmutableOrderedSet;

public class Union {
        
    [Params(
        // 100,
        // 1000,
        // 10000,
        // 30000
        _Utils.ParamsSize
    )]
    public int Count;

    private ImmutableOrderedSet<int> testOrderedSet1;
    private ImmutableOrderedSet<int> testOrderedSet2;
        
    private ImmutableHashSet<int> testHashSet1;
    private ImmutableHashSet<int> testHashSet2;
        
    private ImmutableSortedSet<int> testSortedSet1;
    private ImmutableSortedSet<int> testSortedSet2;

    private ImmutableArray<int> testArray1;
    private ImmutableArray<int> testArray2;

    [GlobalSetup]
    public void Setup() {
        testOrderedSet1 = [..Enumerable.Range(0, Count)];
        testOrderedSet2 = [..Enumerable.Range(Count / 2, Count)];
            
        testHashSet1 = [..Enumerable.Range(0, Count)];
        testHashSet2 = [..Enumerable.Range(Count / 2, Count)];
            
        testSortedSet1 = [..Enumerable.Range(0, Count)];
        testSortedSet2 = [..Enumerable.Range(Count / 2, Count)];
            
        testArray1 = [..Enumerable.Range(0, Count)];
        testArray2 = [..Enumerable.Range(Count / 2, Count)];
    }

    [Benchmark]
    public void ImmutableOrderedSetMerge() {
        _Utils.Use(testOrderedSet1.Union(testOrderedSet2));
    }

    [Benchmark]
    public void ImmutableHashSetMerge() {
        _Utils.Use(testHashSet1.Union(testHashSet2));
    }
        
    [Benchmark]
    public void ImmutableSortedSetMerge() {
        _Utils.Use(testSortedSet1.Union(testSortedSet2));
    }
        
    [Benchmark]
    public void ImmutableArrayMerge() {
        _Utils.Use(testArray1.AddRange(testArray2));
    }
}