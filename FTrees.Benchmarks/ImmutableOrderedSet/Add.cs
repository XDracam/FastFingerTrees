using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DracTec.FTrees.Benchmarks.ImmutableSeq;

namespace DracTec.FTrees.Benchmarks.ImmutableOrderedSet;

public class Add
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
        testOrderedSet = testOrderedSet.Remove(Count / 2);
        testHashSet = [..Enumerable.Range(0, Count)];
        testHashSet = testHashSet.Remove(Count / 2);
        testSortedSet = [..Enumerable.Range(0, Count)];
        testSortedSet = testSortedSet.Remove(Count / 2);
        testArray = [..Enumerable.Range(0, Count)];
        testArray = testArray.Remove(Count / 2);
    }
    
    [Benchmark]
    public ImmutableHashSet<int> ImmutableHashSet_AddStart()
    {
        return testHashSet.Add(-1); // Add a new value.
    }

    [Benchmark] public ImmutableHashSet<int> ImmutableHashSet_AddMiddle()
    {
        return testHashSet.Add(Count / 2); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableHashSet<int> ImmutableHashSet_AddEnd()
    {
        return testHashSet.Add(Count + 1); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableSortedSet<int> ImmutableSortedSet_AddStart()
    {
        return testSortedSet.Add(-1); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableSortedSet<int> ImmutableSortedSet_AddMiddle()
    {
        return testSortedSet.Add(Count / 2); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableSortedSet<int> ImmutableSortedSet_AddEnd()
    {
        return testSortedSet.Add(Count + 1); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableOrderedSet<int> ImmutableOrderedSet_AddStart()
    {
        return testOrderedSet.Add(-1); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableOrderedSet<int> ImmutableOrderedSet_AddMiddle()
    {
        return testOrderedSet.Add(Count / 2); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableOrderedSet<int> ImmutableOrderedSet_AddEnd()
    {
        return testOrderedSet.Add(Count + 1); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableArray<int> ImmutableArray_AddStart()
    {
        return testArray.Add(-1); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableArray<int> ImmutableArray_AddMiddle()
    {
        return testArray.Add(Count / 2); // Add a new value.
    }
    
    [Benchmark]
    public ImmutableArray<int> ImmutableArray_AddEnd()
    {
        return testArray.Add(Count + 1); // Add a new value.
    }
    
}