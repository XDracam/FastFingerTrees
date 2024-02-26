using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace FTrees.Benchmarks {
    public static class Program {
        public static void Main(string[] args) {
            // Run<Add>();
            // Run<CreateRange>();
            // Run<Enumerate>();
            //Run<Index>();

            var count = 10000000;
            var range = Enumerable.Range(0, count);
            var immutableSeq = ImmutableSeq.CreateRange(range);
            for (var i = 0; i < count; ++i) {
                _ = immutableSeq[i];
                _ = immutableSeq[count - i - 1];
            }
        }

        private static void Run<T>() {
            var config = DefaultConfig.Instance.WithArtifactsPath("BenchmarkResults/" + typeof(T).Name)
                .AddExporter(BenchmarkDotNet.Exporters.HtmlExporter.Default)
                .AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub);
            var summary = BenchmarkRunner.Run<T>(config);
        }
    }
}