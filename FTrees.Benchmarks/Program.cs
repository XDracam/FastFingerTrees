using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace DracTec.FTrees.Benchmarks;

public static class Program {
    
    public static void Main(string[] args) {
        // var config = DefaultConfig.Instance.WithArtifactsPath("BenchmarkResults");
        //     
        // var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
        //     
        // var report = switcher.Run(args, config);

        // var sw = Stopwatch.StartNew();
        // var count = 100000;
        //
        // var range = Enumerable.Range(0, count);
        // var immutableSeq = ImmutableSeq.CreateRange(range);
        //
        // sw.Restart();
        //
        // for (var i = 0; i < count; ++i) {
        //     _ = immutableSeq[i];
        //     _ = immutableSeq[count - i - 1];
        // }
        
        var sw = Stopwatch.StartNew();
        var count = 1000000;
        
        var range = Enumerable.Range(0, count);
        var immutableSeq = FTrees.ImmutableSeq.CreateRange(range);
        
        for (var rep = 0; rep < 10; rep++) // reduce impact of CreateRange
         for (var i = 0; i < count; ++i) {
             //immutableSeq = immutableSeq.Insert(immutableSeq.Count, i);
             // TODO: optimize this worst case somehow (allocates way too much!)
             Use(immutableSeq.Insert(count/2, i));
         }
        
        sw.Stop();
        Console.WriteLine(sw.Elapsed);
    }

    // force value to be used
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use<T>(T value) { }
}