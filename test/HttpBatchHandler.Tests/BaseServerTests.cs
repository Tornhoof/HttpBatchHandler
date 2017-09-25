using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HttpBatchHandler.Multipart;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace HttpBatchHandler.Tests
{
    public abstract class BaseServerTests<TFixture> : IClassFixture<TestFixture> where TFixture : TestFixture
    {
        protected readonly TFixture _fixture;

        protected BaseServerTests(TFixture fixture)
        {
            _fixture = fixture;
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