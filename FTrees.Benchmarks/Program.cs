using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace FTrees.Benchmarks {
    public static class Program {
        public static void Main(string[] args) {
            var config = DefaultConfig.Instance.WithArtifactsPath("BenchmarkResults")
                .AddExporter(BenchmarkDotNet.Exporters.HtmlExporter.Default)
                .AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub);
            
            var switcher = BenchmarkSwitcher.FromTypes(new[] {
                typeof(Add),
                typeof(CreateRange),
                typeof(Enumerate),
                typeof(Index),
                typeof(Concat),
                typeof(SelfConcat)
            });
            
            var report = switcher.Run(args, config);

            // var count = 10000000;
            // var range = Enumerable.Range(0, count);
            // var immutableSeq = ImmutableSeq.CreateRange(range);
            // for (var i = 0; i < count; ++i) {
            //     _ = immutableSeq[i];
            //     _ = immutableSeq[count - i - 1];
            // }
        }
    }
}