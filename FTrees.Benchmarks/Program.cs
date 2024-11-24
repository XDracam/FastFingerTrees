using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace DracTec.FTrees.Benchmarks;

public static class Program {
    
    public static void Main(string[] args) {
        var config = DefaultConfig.Instance.WithArtifactsPath("BenchmarkResults");
            
        var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
            
        var report = switcher.Run(args, config);

        // var sw = Stopwatch.StartNew();
        // var count = 100000;
        
        // var range = Enumerable.Range(0, count);
        // var immutableSeq = ImmutableSeq.CreateRange(range);
        //
        // sw.Restart();
        //
        // for (var i = 0; i < count; ++i) {
        //     _ = immutableSeq[i];
        //     _ = immutableSeq[count - i - 1];
        // }
        // //
        // // var count = 1000000;
        // // var range = Enumerable.Range(0, count);
        // // var immutableSeq = ImmutableSeq.CreateRange(range);
        // //
        // // for (var i = 0; i < count; ++i) {
        // //     //immutableSeq = immutableSeq.Insert(immutableSeq.Count, i);
        // //     // TODO: optimize this worst case somehow
        // //     immutableSeq = immutableSeq.Insert(i/2, i);
        // // }
        //
        // var set = FTrees.ImmutableOrderedSet.CreateRange(Enumerable.Range(0, count));
        //
        // for (var i = 0; i < count; ++i)
        //     _ = set.Remove(count / 2);
        //
        // sw.Stop();
        // Console.WriteLine(sw.Elapsed);
    }
}