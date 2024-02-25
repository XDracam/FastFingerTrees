using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace FTrees.Benchmarks {
    public class Index {
        private List<int> list;
        private ImmutableArray<int> immutableArray;
        private ImmutableList<int> immutableList;
        private ImmutableSeq<int> immutableSeq;

        [Params(100, 1000, 10000, 30000)]
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            var range = Enumerable.Range(0, Count).ToArray();
            list = new List<int>(range);
            immutableArray = ImmutableArray.CreateRange(range);
            immutableList = ImmutableList.CreateRange(range);
            immutableSeq = ImmutableSeq.CreateRange(range);
        }
        
        [Benchmark]
        public void ListIndex() {
            for (var i = 0; i < Count; ++i)
                _ = list[i];
        }
        
        [Benchmark]
        public void ImmutableArrayIndex()
        {
            for (var i = 0; i < Count; ++i)
                _ = immutableArray[i];
        }
        
        [Benchmark]
        public void ImmutableListIndex()
        {
            for (var i = 0; i < Count; ++i)
                _ = immutableList[i];
        }
        
        [Benchmark]
        public void ImmutableSeqIndex()
        {
            for (var i = 0; i < Count; ++i)
                _ = immutableSeq[i];
        }
    }
}