using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HttpBatchHandler.Benchmarks.Tools;
using HttpBatchHandler.Multipart;
using Microsoft.AspNetCore.WebUtilities;

namespace HttpBatchHandler.Benchmarks
{
    [MemoryDiagnoser]
    public class KestrelBenchmark
    {
        private TestHost _testHost;
        [GlobalSetup]
        public void GlobalSetup()
        {
            _testHost = new TestHost();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _testHost.Dispose();
        }

        [Benchmark]
        public async Task<int> Requests()
        {
            var count = 1000;
            var messages = new List<HttpRequestMessage>(count);
            for (var j = 0; j < count; j++)
            {
                var message = new HttpRequestMessage(HttpMethod.Get, new Uri(_testHost.BaseUri, "api/values"));
                messages.Add(message);
            }

            var result = await SendBatchRequestAsync(messages).ConfigureAwait(false);
            return result;
        }

        private async Task<int> SendBatchRequestAsync(IEnumerable<HttpRequestMessage> requestMessages,  CancellationToken cancellationToken = default)
        {
            var batchUri = new Uri(_testHost.BaseUri, "api/batch");
            int count = 0;
            using (var requestContent = new MultipartContent("batch", "batch_" + Guid.NewGuid()))
            {
                var multipartContent = requestContent;
                foreach (var httpRequestMessage in requestMessages)
                {
                    var content = new HttpApplicationContent(httpRequestMessage);
                    multipartContent.Add(content);
                }

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, batchUri)
                {
                    Content = requestContent
                })
                {
                    using (var responseMessage = await _testHost.HttpClient.SendAsync(requestMessage, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        var response = await responseMessage.Content.ReadAsMultipartAsync(cancellationToken)
                            .ConfigureAwait(false);
                        MultipartSection section;
                        while ((section = await response.ReadNextSectionAsync(cancellationToken)) != null)
                        {
                            count++;
                        }
                    }
                }
            }
            return count;
        }
    }
}
