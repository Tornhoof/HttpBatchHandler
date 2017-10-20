using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpBatchHandler.Multipart;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace HttpBatchHandler.Tests
{
    public abstract class BaseServerTests<TFixture> : IClassFixture<TFixture> where TFixture : TestFixture
    {
        protected readonly TFixture _fixture;

        protected BaseServerTests(TFixture fixture, ITestOutputHelper outputHelper)
        {
            _fixture = fixture;
            _outputHelper = outputHelper;
        }


        private readonly ITestOutputHelper _outputHelper;

        [Fact]
        public async Task Performance()
        {
            for (var i = 0; i < 10; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                var count = 1000;
                var messages = new List<HttpRequestMessage>(count);
                for (var j = 0; j < count; j++)
                {
                    var message = new HttpRequestMessage(HttpMethod.Get, new Uri(_fixture.BaseUri, "api/values"));
                    messages.Add(message);
                }
                var result = await SendBatchRequestAsync<StringBatchResult>(messages).ConfigureAwait(false);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal(1000, result.ResponsePayload.Length);
                sw.Stop();
                _outputHelper.WriteLine("Time:  {0}", sw.Elapsed.TotalMilliseconds);
            }
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
            var result = await SendBatchRequestAsync<StringBatchResult>(messages).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(6, result.ResponsePayload.Length);
            foreach (var batchResult in result.ResponsePayload)
            {
                Assert.True(batchResult.IsSuccessStatusCode);
            }
            var createdResult = result.ResponsePayload[3];
            Assert.Equal(201, createdResult.StatusCode);
            var locationHeader = createdResult.Headers["Location"];
            var comparisonUri = new Uri(_fixture.BaseUri, "api/Values/5");
            Assert.Equal(comparisonUri.ToString(), locationHeader);
        }

        /// <summary>
        /// This is basically to find out if the requestStream is correct and not extended with random data
        /// </summary>
        [Fact]
        public async Task FileUpload()
        {
            var messages = new[]
            {
                new HttpRequestMessage(HttpMethod.Post, new Uri(_fixture.BaseUri, "api/values/File/1"))
                {
                    Content = RandomStreamContent()
                },
                new HttpRequestMessage(HttpMethod.Post, new Uri(_fixture.BaseUri, "api/values/File/2"))
                {
                    Content = RandomStreamContent()
                },
                new HttpRequestMessage(HttpMethod.Post, new Uri(_fixture.BaseUri, "api/values/File/3"))
                {
                    Content = RandomStreamContent()
                }
            };
            var result = await SendBatchRequestAsync<StringBatchResult>(messages).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(3, result.ResponsePayload.Length);
            foreach (var batchResult in result.ResponsePayload)
            {
                Assert.True(batchResult.IsSuccessStatusCode);
            }
        }

        private MultipartFormDataContent RandomStreamContent()
        {
            var random = new Random();
            var ms = new MemoryStream();
            for (int i = 0; i < 10; i++)
            {
                var buffer = new byte[1 << 16];
                random.NextBytes(buffer);
                ms.Write(buffer, 0, buffer.Length);
            }
            string b64Name;
            ms.Position = 0;
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(ms);
                b64Name = Convert.ToBase64String(hash);
            }
            ms.Position = 0;
            var streamContent = new StreamContent(ms);
            return new MultipartFormDataContent { { streamContent, b64Name, b64Name } };
        }

        /// <summary>
        /// This is basically to find out if the responseStream is correct and not extended with random data
        /// </summary>
        [Fact]
        public async Task FileDownload()
        {
            var messages = new[]
            {
                new HttpRequestMessage(HttpMethod.Get, new Uri(_fixture.BaseUri, "api/values/File")),
                new HttpRequestMessage(HttpMethod.Get, new Uri(_fixture.BaseUri, "api/values/File")),
                new HttpRequestMessage(HttpMethod.Get, new Uri(_fixture.BaseUri, "api/values/File"))
            };
            var result = await SendBatchRequestAsync<StreamBatchResult>(messages).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(3, result.ResponsePayload.Length);
            foreach (var batchResult in result.ResponsePayload)
            {
                Assert.True(batchResult.IsSuccessStatusCode);
                var streamBatchResult = (StreamBatchResult)batchResult;
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(streamBatchResult.ResponsePayload);
                    var b64 = Convert.ToBase64String(hash);
                    Assert.True(
                        streamBatchResult.Headers.TryGetValue(HeaderNames.ContentDisposition, out var contentDispo));
                    Assert.Single(contentDispo);
                    Assert.Contains(b64, contentDispo.First());
                }
                streamBatchResult.ResponsePayload.Dispose();
            }
        }

        protected async Task<BatchResults> SendBatchRequestAsync<TBatchResult>(
            IEnumerable<HttpRequestMessage> requestMessages,
            CancellationToken cancellationToken = default(CancellationToken)) where TBatchResult : BatchResult, new()
        {
            var batchUri = new Uri(_fixture.BaseUri, "api/batch");
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
                    using (var responseMessage = await _fixture.HttpClient.SendAsync(requestMessage, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        var response = await responseMessage.Content.ReadAsMultipartAsync(cancellationToken)
                            .ConfigureAwait(false);
                        var responsePayload =
                            await ReadResponseAsync<TBatchResult>(response, cancellationToken).ConfigureAwait(false);
                        var statusCode = responseMessage.StatusCode;
                        return new BatchResults {ResponsePayload = responsePayload, StatusCode = statusCode};
                    }
                }
            }
        }

        private async Task<TBatchResult[]> ReadResponseAsync<TBatchResult>(MultipartReader reader,
            CancellationToken cancellationToken = default(CancellationToken)) where TBatchResult : BatchResult, new()
        {
            var result = new List<TBatchResult>();
            HttpApplicationResponseSection section;
            while ((section = await reader.ReadNextHttpApplicationResponseSectionAsync(cancellationToken)) !=
                   null)
            {
                var batchResult = new TBatchResult
                {
                    StatusCode = section.ResponseFeature.StatusCode,
                    Headers = section.ResponseFeature.Headers
                };
                await batchResult.FillAsync(section.ResponseFeature.Body);
                result.Add(batchResult);
            }
            return result.ToArray();
        }

        protected abstract class BatchResult
        {
            public IHeaderDictionary Headers { get; set; }
            public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;
            public int StatusCode { get; set; }

            public abstract Task FillAsync(Stream data);
        }

        protected class StringBatchResult : BatchResult
        {
            public string ResponsePayload { get; private set; }

            public override async Task FillAsync(Stream data)
            {
                using (data)
                {
                    ResponsePayload = await data.ReadAsStringAsync();
                }
            }
        }

        protected class StreamBatchResult : BatchResult
        {
            public Stream ResponsePayload { get; private set; }

            public override async Task FillAsync(Stream data)
            {
                ResponsePayload = new MemoryStream();
                using (data)
                {
                    await data.CopyToAsync(ResponsePayload);
                }
                ResponsePayload.Position = 0;
            }
        }

        protected class BatchResults
        {
            public BatchResult[] ResponsePayload { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }
    }
}