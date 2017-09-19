using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
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
                var result = await SendBatchRequestAsync(messages).ConfigureAwait(false);
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
            var result = await SendBatchRequestAsync(messages).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(6, result.ResponsePayload.Length);
            foreach (var batchResult in result.ResponsePayload)
            {
                Assert.True(batchResult.IsSuccessStatusCode);
            }
        }
    }
}