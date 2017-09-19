using System;
using System.Collections.Generic;
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

        protected async Task<BatchResults> SendBatchRequestAsync(IEnumerable<HttpRequestMessage> requestMessages,
            CancellationToken cancellationToken = default(CancellationToken))
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
                            await ReadResponseAsync(response, cancellationToken).ConfigureAwait(false);
                        var statusCode = responseMessage.StatusCode;
                        return new BatchResults {ResponsePayload = responsePayload, StatusCode = statusCode};
                    }
                }
            }
        }

        private async Task<BatchResult[]> ReadResponseAsync(MultipartReader reader,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new List<BatchResult>();
            HttpApplicationResponseSection section;
            while ((section = await reader.ReadNextHttpApplicationResponseSectionAsync(cancellationToken)) !=
                   null)
            {
                var content = await section.ReadAsStringAsync(cancellationToken);
                result.Add(new BatchResult
                {
                    ResponsePayload = content,
                    StatusCode = section.ResponseFeature.StatusCode,
                    Headers = section.ResponseFeature.Headers
                });
            }
            return result.ToArray();
        }

        protected class BatchResult
        {
            public IHeaderDictionary Headers { get; set; }
            public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;
            public string ResponsePayload { get; set; }
            public int StatusCode { get; set; }
        }

        protected class BatchResults
        {
            public BatchResult[] ResponsePayload { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }
    }
}