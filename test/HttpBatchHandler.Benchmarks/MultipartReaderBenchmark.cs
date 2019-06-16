using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HttpBatchHandler.Multipart;
using Microsoft.AspNetCore.WebUtilities;

namespace HttpBatchHandler.Benchmarks
{
    [MemoryDiagnoser]
    public class MultipartReaderBenchmark
    {
        private const string Boundary = "batch_45cdcaaf-774f-40c6-8a12-dbb835d3132e";

        [Benchmark]
        public async Task<int> ReadMultipart()
        {
            var counter = 0;
            using (var body = GetPayload())
            {
                var reader = new MultipartReader(Boundary, body);
                HttpApplicationRequestSection section;
                while ((section = await reader
                           .ReadNextHttpApplicationRequestSectionAsync(default, false, CancellationToken.None)
                           .ConfigureAwait(false)) != null)
                    counter++;
            }

            return counter;
        }

        private Stream GetPayload()
        {
            var type = typeof(MultipartReaderBenchmark);
            return type.Assembly.GetManifestResourceStream(type, "payload.bin");
        }
    }
}