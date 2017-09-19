using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace HttpBatchHandler.Tests
{
    public class ServerTests : BaseServerTests<TestFixture>
    {
        public ServerTests(TestFixture fixture, ITestOutputHelper outputHelper) : base(fixture)
        {
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
            return new MultipartFormDataContent {{streamContent, b64Name, b64Name}};
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
                var streamBatchResult = (StreamBatchResult) batchResult;
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
    }
}