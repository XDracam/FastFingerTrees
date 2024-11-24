using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DracTec.FTrees.Benchmarks.ImmutableSeq;

namespace DracTec.FTrees.Benchmarks.ImmutableOrderedSet;

public class Remove
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
    public ImmutableHashSet<int> ImmutableHashSet_RemoveStart()
    {
        return testHashSet.Remove(0);
    }
    
    [Benchmark]
    public ImmutableHashSet<int> ImmutableHashSet_RemoveMiddle()
    {
        return testHashSet.Remove(Count / 2);
    }
    
    [Benchmark]
    public ImmutableHashSet<int> ImmutableHashSet_RemoveEnd()
    {
        return testHashSet.Remove(Count - 1);
    }

    

    [Benchmark]
    public ImmutableSortedSet<int> ImmutableSortedSet_RemoveStart()
    {
        return testSortedSet.Remove(0);
    }
    
    [Benchmark]
    public ImmutableSortedSet<int> ImmutableSortedSet_RemoveMiddle()
    {
        return testSortedSet.Remove(Count / 2);
    }
    
    [Benchmark]
    public ImmutableSortedSet<int> ImmutableSortedSet_RemoveEnd()
    {
        return testSortedSet.Remove(Count - 1);
    }
    
    

    [Benchmark]
    public ImmutableOrderedSet<int> ImmutableOrderedSet_RemoveStart()
    {
        return testOrderedSet.Remove(0);
    }
    
    [Benchmark]
    public ImmutableOrderedSet<int> ImmutableOrderedSet_RemoveMiddle()
    {
        return testOrderedSet.Remove(Count / 2);
    }
    [Benchmark]
    public ImmutableOrderedSet<int> ImmutableOrderedSet_RemoveEnd()
    {
        return testOrderedSet.Remove(Count - 1);
    }
    

    [Benchmark]
    public ImmutableArray<int> ImmutableArray_RemoveStart()
    {
        return testArray.Remove(0);
    }
    
    [Benchmark]
    public ImmutableArray<int> ImmutableArray_RemoveMiddle()
    {
        return testArray.Remove(Count / 2);
    }
    
    [Benchmark]
    public ImmutableArray<int> ImmutableArray_RemoveEnd()
    {
        return testArray.Remove(Count - 1);
    }
}