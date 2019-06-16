using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace HttpBatchHandler.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<MultipartReaderBenchmark>();
        }
    }
}