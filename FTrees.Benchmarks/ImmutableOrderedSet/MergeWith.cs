using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace FTrees.Benchmarks {
    public class MergeWith {
        
        [Params(
            100,
            1000,
            10000,
            30000
        )]
        public int Count;

        private ImmutableOrderedSet<int> testOrderedSet1;
        private ImmutableOrderedSet<int> testOrderedSet2;
        
        private ImmutableHashSet<int> testHashSet1;
        private ImmutableHashSet<int> testHashSet2;

        private ImmutableArray<int> testArray1;
        private ImmutableArray<int> testArray2;

        [GlobalSetup]
        public void Setup() {
            testOrderedSet1 = ImmutableOrderedSet.CreateRange(Enumerable.Range(0, Count));
            testOrderedSet2 = ImmutableOrderedSet.CreateRange(Enumerable.Range(Count / 2, Count));
            
            testHashSet1 = ImmutableHashSet.CreateRange(Enumerable.Range(0, Count));
            testHashSet2 = ImmutableHashSet.CreateRange(Enumerable.Range(Count / 2, Count));
            
            testArray1 = ImmutableArray.CreateRange(Enumerable.Range(0, Count));
            testArray2 = ImmutableArray.CreateRange(Enumerable.Range(Count / 2, Count));
        }

        [Benchmark]
        public void OrderedSetMerge() {
            _ = testOrderedSet1.MergeWith(testOrderedSet2);
        }

        [Benchmark]
        public void HashSetMerge() {
            _ = testHashSet1.Union(testHashSet2);
        }

        [Benchmark]
        public void ArrayMerge() {
            _ = testArray1.AddRange(testArray2);
        }
    }
}