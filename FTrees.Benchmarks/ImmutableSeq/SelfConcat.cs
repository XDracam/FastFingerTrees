using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace FTrees.Benchmarks {
    public class SelfConcat {
        
        private List<int> list;
        private ImmutableArray<int> immutableArray;
        private ImmutableList<int> immutableList;
        private ImmutableSeq<int> immutableSeq;

        [Params(
            // 100,
            // 1000,
            10000//,
            // 30000
        )]
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
        public void ListConcat() {
            _ = list.Concat(list).ToList(); // "immutable concat"
        }
        
        [Benchmark]
        public void ImmutableArrayConcat() {
            _ = immutableArray.AddRange(immutableArray);
        }
        
        [Benchmark]
        public void ImmutableListConcat() {
            _ = immutableList.AddRange(immutableList);
        }
        
        [Benchmark]
        public void ImmutableSeqConcat()
        {
            _ = immutableSeq.Concat(immutableSeq);
        }
    }
}