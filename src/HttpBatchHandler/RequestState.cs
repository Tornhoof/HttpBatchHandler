using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    internal class RequestState
    {
        private readonly IHttpContextFactory _factory;
        private readonly CancellationTokenSource _requestAbortedSource;
        private readonly IHttpRequestFeature _requestFeature;
        private readonly ResponseFeature _responseFeature;
        private readonly ResponseStream _responseStream;
        private readonly TaskCompletionSource<HttpApplicationContent> _responseTcs;
        private bool _pipelineFinished;

        internal RequestState(IHttpRequestFeature requestFeature, IHttpContextFactory factory,
            IServiceProvider provider)
        {
            _requestFeature = requestFeature;
            _factory = factory;
            _responseTcs = new TaskCompletionSource<HttpApplicationContent>();
            _requestAbortedSource = new CancellationTokenSource();
            _pipelineFinished = false;

            var contextFeatures = new FeatureCollection();
            contextFeatures.Set(requestFeature);
            _responseFeature = new ResponseFeature();
            contextFeatures.Set<IHttpResponseFeature>(_responseFeature);
            var requestLifetimeFeature = new HttpRequestLifetimeFeature();
            contextFeatures.Set<IHttpRequestLifetimeFeature>(requestLifetimeFeature);
            var serviceProvidersFeature = new ServiceProvidersFeature {RequestServices = provider};
            contextFeatures.Set<IServiceProvidersFeature>(serviceProvidersFeature);

            _responseStream = new ResponseStream(ReturnResponseMessageAsync, AbortRequest);
            _responseFeature.Body = _responseStream;
            _responseFeature.StatusCode = 200;
            requestLifetimeFeature.RequestAborted = _requestAbortedSource.Token;

            Context = _factory.Create(contextFeatures);
        }

        public HttpContext Context { get; }

        public Task<HttpApplicationContent> ResponseTask => _responseTcs.Task;

        internal void AbortRequest()
        {
            if (!_pipelineFinished)
            {
                _requestAbortedSource.Cancel();
            }
            _responseStream.Complete();
        }

        internal async Task CompleteResponseAsync()
        {
            _pipelineFinished = true;
            await ReturnResponseMessageAsync();
            _responseStream.Complete();
            await _responseFeature.FireOnResponseCompletedAsync();
        }

        internal async Task ReturnResponseMessageAsync()
        {
            // Check if the response has already started because the TrySetResult below could happen a bit late
            // (as it happens on a different thread) by which point the CompleteResponseAsync could run and calls this
            // method again.
            if (!Context.Response.HasStarted)
            {
                var response = await GenerateResponseAsync();
                // Dispatch, as TrySetResult will synchronously execute the waiters callback and block our Write.
                var setResult = Task.Factory.StartNew(() => _responseTcs.TrySetResult(response));
            }
        }

        private async Task<HttpApplicationContent> GenerateResponseAsync()
        {
            await _responseFeature.FireOnSendingHeadersAsync();

            var response = new HttpApplicationContent
            (
                _requestFeature.Protocol,
                Context.Response.StatusCode,
                Context.Features.Get<IHttpResponseFeature>().ReasonPhrase,
                _responseStream,
                Context.Features.Get<IHttpResponseFeature>().Headers
            );
            return response;
        }

        internal void Abort(Exception exception)
        {
            _pipelineFinished = true;
            _responseStream.Abort(exception);
            _responseTcs.TrySetException(exception);
        }

        internal void ServerCleanup()
        {
            _factory.Dispose(Context);
        }
    }
}