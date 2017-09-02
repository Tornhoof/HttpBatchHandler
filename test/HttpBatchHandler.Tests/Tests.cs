using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HttpBatchHandler.Tests
{
    public class Tests : IClassFixture<TestFixture>
    {
        public Tests(TestFixture fixture, ITestOutputHelper outputHelper)
        {
            _fixture = fixture;
            _outputHelper = outputHelper;
        }

        private readonly TestFixture _fixture;
        private readonly ITestOutputHelper _outputHelper;

        protected async Task<BatchResults> SendBatchRequestAsync(IEnumerable<HttpRequestMessage> requestMessages,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var batchUri = new Uri(_fixture.BaseUri, "api/batch");
            using (var requestContent = new MultipartContent("batch", "batch_" + Guid.NewGuid()))
            {
                var multipartContent = requestContent;
                foreach (var httpRequestMessage in requestMessages)
                {
                    var content = new HttpMessageContent(httpRequestMessage);
                    multipartContent.Add(content);
                }
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, batchUri)
                {
                    Content = requestContent
                })
                {
                    using (var responseMessage = await _fixture.HttpClient.SendAsync(requestMessage, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        var response = await responseMessage.Content.ReadAsMultipartAsync(cancellationToken)
                            .ConfigureAwait(false);
                        var responsePayload =
                            await ReadResponseAsync(response, cancellationToken).ConfigureAwait(false);
                        var statusCode = responseMessage.StatusCode;
                        return new BatchResults {ResponsePayload = responsePayload, StatusCode = statusCode};
                    }
                }
            }
        }

        protected class BatchResults
        {
            public BatchResult[] ResponsePayload { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }

        protected class BatchResult
        {
            public string ResponsePayload { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            public HttpResponseHeaders ResponseHeaders { get; set; }
            public HttpContentHeaders ContentHeaders { get; set; }

            public bool IsSuccessStatusCode => (int) StatusCode >= 200 && (int) StatusCode <= 299;
        }

        private async Task<BatchResult[]> ReadResponseAsync(MultipartMemoryStreamProvider provider,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new List<BatchResult>();
            foreach (var httpContent in provider.Contents)
            {
                if (httpContent.IsMimeMultipartContent())
                {
                    var response =
                        await ReadResponseAsync(
                            await httpContent.ReadAsMultipartAsync(cancellationToken).ConfigureAwait(false),
                            cancellationToken).ConfigureAwait(false);
                    result.AddRange(response);
                }
                else
                {
                    var message = await httpContent.ReadAsHttpResponseMessageAsync(cancellationToken)
                        .ConfigureAwait(false);
                    string content = null;
                    if (message.Content != null)
                    {
                        content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                    result.Add(new BatchResult
                    {
                        ResponsePayload = content,
                        StatusCode = message.StatusCode,
                        ResponseHeaders = message.Headers,
                        ContentHeaders = message.Content?.Headers
                    });
                }
            }
            return result.ToArray();
        }

        protected HttpRequestMessage BuildBatchMessage(HttpMethod method, Uri uri, string payload)
        {
            var message = new HttpRequestMessage(method, uri)
            {
                Content = payload != null ? new StringContent(payload, Encoding.UTF8, "application/json") : null
            };
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return message;
        }

        [Fact]
        public async Task Test()
        {
            var messages = new[]
            {
                new HttpRequestMessage(HttpMethod.Get, new Uri(_fixture.BaseUri, "api/values")),
                new HttpRequestMessage(HttpMethod.Get, new Uri(_fixture.BaseUri, "api/values/5")),
                new HttpRequestMessage(HttpMethod.Get, new Uri(_fixture.BaseUri, "api/values/query?id=5")),
                new HttpRequestMessage(HttpMethod.Post, new Uri(_fixture.BaseUri, "api/values"))
                {
                    Content = new StringContent("{\"value\": \"Hello World\"}", Encoding.UTF8, "application/json")
                },
                new HttpRequestMessage(HttpMethod.Put, new Uri(_fixture.BaseUri, "api/values/5"))
                {
                    Content = new StringContent("{\"value\": \"Hello World\"}", Encoding.UTF8, "application/json")
                },
                new HttpRequestMessage(HttpMethod.Delete, new Uri(_fixture.BaseUri, "api/values/5"))
            };
            var result = await SendBatchRequestAsync(messages).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(6, result.ResponsePayload.Length);
            foreach (var batchResult in result.ResponsePayload)
            {
                Assert.True(batchResult.IsSuccessStatusCode);
            }
        }

        [Fact]
        public async Task Performance()
        {
            for (int i = 0; i < 10; i++)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                int count = 10000;
                var messages = new List<HttpRequestMessage>(count);
                for (int j = 0; j < count; j++)
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, new Uri(_fixture.BaseUri, "api/values"));
                    messages.Add(message);
                }
                var result = await SendBatchRequestAsync(messages).ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal(10000, result.ResponsePayload.Length);
                sw.Stop();
                _outputHelper.WriteLine("Time:  {0}", sw.Elapsed.TotalMilliseconds);
            }
        }
    }
}