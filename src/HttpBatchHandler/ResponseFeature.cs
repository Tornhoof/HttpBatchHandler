using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    internal class ResponseFeature : IHttpResponseFeature
    {
        private Func<Task> _responseCompletedAsync = () => Task.FromResult(true);
        private Func<Task> _responseStartingAsync = () => Task.FromResult(true);

        internal ResponseFeature(string protocol, int statusCode, string reasonPhrase, Stream content,
            IHeaderDictionary headers)
        {
            Protocol = protocol;
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Body = content;
            Headers = headers;
        }

        public Stream Stream => Body;

        public Action<Exception> Abort { get; set; }

        public Stream Body { get; set; }

        public bool HasStarted { get; private set; }

        public IHeaderDictionary Headers { get; set; }

        public string Protocol { get; set; }

        public string ReasonPhrase { get; set; }

        public int StatusCode { get; set; }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            var prior = _responseCompletedAsync;
            _responseCompletedAsync = async () =>
            {
                try
                {
                    await callback(state).ConfigureAwait(false);
                }
                finally
                {
                    await prior().ConfigureAwait(false);
                }
            };
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            if (HasStarted)
            {
                throw new InvalidOperationException();
            }

            var prior = _responseStartingAsync;
            _responseStartingAsync = async () =>
            {
                await callback(state).ConfigureAwait(false);
                await prior().ConfigureAwait(false);
            };
        }

        public Task FireOnResponseCompletedAsync() => _responseCompletedAsync();

        public async Task FireOnSendingHeadersAsync()
        {
            if (!HasStarted)
            {
                try
                {
                    await _responseStartingAsync().ConfigureAwait(false);
                }
                finally
                {
                    HasStarted = true;
                    if (Headers is HeaderDictionary hd)
                    {
                        hd.IsReadOnly = true;
                    }
                }
            }
        }
    }
}