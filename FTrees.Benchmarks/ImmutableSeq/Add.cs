using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace FTrees.Benchmarks {
    public class Add {

        [Params(
            // 100,
            // 1000,
            // 10000,
            // 30000
            Constants.ParamsSize
        )]
        public int Count;

        [GlobalSetup]
        public void Setup() { }

        // [Benchmark]
        // public void ListAdd() {
        //     var list = new List<int>();
        //     for (var i = 0; i < Count; ++i)
        //         list.Add(i);
        // }
        //
        // [Benchmark]
        // public void ImmutableArrayAdd()
        // {
        //     var list = ImmutableArray<int>.Empty;
        //     for (var i = 0; i < Count; ++i)
        //         list = list.Add(i);
        // }
        //
        // [Benchmark]
        // public void ImmutableListAdd()
        // {
        //     var list = ImmutableList<int>.Empty;
        //     for (var i = 0; i < Count; ++i)
        //         list = list.Add(i);
        // }
        
        [Benchmark]
        public void ImmutableSeqAdd()
        {
            var list = ImmutableSeq<int>.Empty;
            for (var i = 0; i < Count; ++i)
                list = list.Add(i);
        }
    }
}