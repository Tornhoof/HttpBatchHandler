using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace HttpBatchHandler.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //KestrelBenchmark kb = new KestrelBenchmark();
            //kb.GlobalSetup();
            //await kb.Requests().ConfigureAwait(false);
            //kb.GlobalCleanup();
            BenchmarkSwitcher.FromAssemblies(new[] {typeof(Program).Assembly}).Run();
        }
    }
}