using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    internal class ResponseFeature : IHttpResponseFeature
    {
        private Func<Task> _responseCompletedAsync = () => Task.FromResult(true);
        private Func<Task> _responseStartingAsync = () => Task.FromResult(true);

        internal ResponseFeature(string httpVersion, int statusCode, string reasonPhrase, Stream content,
            IHeaderDictionary headers)
        {
            Protocol = httpVersion;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Body = content;
            Headers = headers;
        }

        public ResponseFeature(string protocol) : this(protocol, StatusCodes.Status200OK, null, new MemoryStream(),
            new HeaderDictionary())
        {
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public Stream Body { get; set; }

        public bool HasStarted { get; private set; }

        public string Protocol { get; set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            if (HasStarted)
            {
                throw new InvalidOperationException();
            }

            var prior = _responseStartingAsync;
            _responseStartingAsync = async () =>
            {
                await callback(state);
                await prior();
            };
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            var prior = _responseCompletedAsync;
            _responseCompletedAsync = async () =>
            {
                try
                {
                    await callback(state);
                }
                finally
                {
                    await prior();
                }
            };
        }

        public async Task FireOnSendingHeadersAsync()
        {
            await _responseStartingAsync();
            HasStarted = true;
        }

        public Task FireOnResponseCompletedAsync()
        {
            return _responseCompletedAsync();
        }
    }
}