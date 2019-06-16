using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HttpBatchHandler.Multipart;
using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler.Benchmarks
{
    [MemoryDiagnoser]
    public class MultipartWriterBenchmark
    {
        private const string Boundary = "batch_45cdcaaf-774f-40c6-8a12-dbb835d3132e";


        [Benchmark]
        public async Task WriteMultipart()
        {
            using (var writer = new MultipartWriter("mixed", Boundary))
            {
                var headerDictionary = new HeaderDictionary {{"Content-Type", "application/json; charset=utf-8"}};
                for (int i = 0; i < 1000; i++)
                {
                    writer.Add(new HttpApplicationMultipart("2.0", 200, "Ok",
                        new MemoryStream(Encoding.UTF8.GetBytes(i.ToString())), headerDictionary));
                }
                await writer.CopyToAsync(Stream.Null, CancellationToken.None);
            }
        }
    }
}
