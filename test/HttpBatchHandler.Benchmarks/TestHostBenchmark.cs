//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;
//using BenchmarkDotNet.Attributes;
//using HttpBatchHandler.Benchmarks.Tools;
//using HttpBatchHandler.Multipart;
//using Microsoft.AspNetCore.WebUtilities;

//namespace HttpBatchHandler.Benchmarks
//{
//    [MemoryDiagnoser]
//    public class TestHostBenchmark
//    {
//        private TestHost _kestrelHost;
//        [GlobalSetup]
//        public void GlobalSetup()
//        {
//            _kestrelHost = new TestHost();
//        }

//        [GlobalCleanup]
//        public void GlobalCleanup()
//        {
//            _kestrelHost.Dispose();
//        }

//        [Benchmark]
//        public async Task<int> Requests()
//        {
//            var count = 1000;
//            var messages = new List<HttpRequestMessage>(count);
//            for (var j = 0; j < count; j++)
//            {
//                var message = new HttpRequestMessage(HttpMethod.Get, new Uri(_kestrelHost.BaseUri, "api/values"));
//                messages.Add(message);
//            }

//            var result = await SendBatchRequestAsync(messages).ConfigureAwait(false);
//            return result;
//        }

//        private async Task<int> SendBatchRequestAsync(IEnumerable<HttpRequestMessage> requestMessages,  CancellationToken cancellationToken = default)
//        {
//            var batchUri = new Uri(_kestrelHost.BaseUri, "api/batch");
//            int count = 0;
//            using (var requestContent = new MultipartContent("batch", "batch_" + Guid.NewGuid()))
//            {
//                var multipartContent = requestContent;
//                foreach (var httpRequestMessage in requestMessages)
//                {
//                    var content = new HttpApplicationContent(httpRequestMessage);
//                    multipartContent.Add(content);
//                }

//                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, batchUri)
//                {
//                    Content = requestContent
//                })
//                {
//                    using (var responseMessage = await _kestrelHost.HttpClient.SendAsync(requestMessage, cancellationToken)
//                        .ConfigureAwait(false))
//                    {
//                        var reader = await Multipart.HttpContentExtensions.ReadAsMultipartAsync(responseMessage.Content, cancellationToken).ConfigureAwait(false);
//                        MultipartSection section;
//                        while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
//                        {
//                            count++;
//                        }
//                    }
//                }
//            }
//            return count;
//        }
//    }
//}
