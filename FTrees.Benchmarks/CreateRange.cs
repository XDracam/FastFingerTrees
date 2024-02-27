using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace FTrees.Benchmarks {
    public class CreateRange {
        
        private int[] testData;

        [Params(
            // 100,
            // 1000,
            // 10000,
            30000
        )]
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            testData = new int[Count];
            for (int i = 0; i < testData.Length; i++)
                testData[i] = i;
        }
        
        [Benchmark]
        public void ListCreateRange()
        {
            var list = new List<int>(testData);
        }
        
        [Benchmark]
        public void ImmutableArrayCreateRange()
        {
            var list = ImmutableArray.CreateRange(testData);
        }
        
        [Benchmark]
        public void ImmutableListCreateRange()
        {
            var list = ImmutableList.CreateRange(testData);
        }
        
        [Benchmark]
        public void ImmutableSeqCreateRange()
        {
            var list = ImmutableSeq.CreateRange(testData);
        }
    }
}