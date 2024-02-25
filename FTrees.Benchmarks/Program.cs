using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace FTrees.Benchmarks {
    public static class Program {
        public static void Main(string[] args) {
            Run<Add>();
            Run<CreateRange>();
            Run<Enumerate>();
        }

        private static void Run<T>() {
            var config = DefaultConfig.Instance.WithArtifactsPath("BenchmarkResults/" + typeof(T).Name)
                .AddExporter(BenchmarkDotNet.Exporters.HtmlExporter.Default)
                .AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub);
            var summary = BenchmarkRunner.Run<T>(config);
        }
    }
}