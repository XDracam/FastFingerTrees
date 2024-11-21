using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace FTrees.Benchmarks {
    public class InsertMiddle {

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
        // public void ListInsertMiddle() {
        //     var list = new List<int>();
        //     for (var i = 0; i < Count; ++i)
        //         list.Insert(i / 2, i);
        // }
        //
        // [Benchmark]
        // public void ImmutableArrayInsertMiddle()
        // {
        //     var list = ImmutableArray<int>.Empty;
        //     for (var i = 0; i < Count; ++i)
        //         list = list.Insert(i / 2, i);
        // }
        //
        // [Benchmark]
        // public void ImmutableListInsertMiddle()
        // {
        //     var list = ImmutableList<int>.Empty;
        //     for (var i = 0; i < Count; ++i)
        //         list = list.Insert(i / 2, i);
        // }
        
        [Benchmark]
        public void ImmutableSeqInsertMiddle()
        {
            var list = ImmutableSeq<int>.Empty;
            for (var i = 0; i < Count; ++i)
                list = list.Insert(i / 2, i);
        }
    }
}