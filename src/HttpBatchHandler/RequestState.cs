using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    internal class RequestState : IDisposable
    {
        private readonly IHttpContextFactory _factory;
        private readonly CancellationTokenSource _requestAbortedSource;
        private readonly IHttpRequestFeature _requestFeature;
        private readonly ResponseFeature _responseFeature;
        private readonly WriteOnlyResponseStream _responseStream;
        private bool _pipelineFinished;

        internal RequestState(IHttpRequestFeature requestFeature, IHttpContextFactory factory,
            FeatureCollection featureCollection)
        {
            _requestFeature = requestFeature;
            _factory = factory;
            _requestAbortedSource = new CancellationTokenSource();
            _pipelineFinished = false;

            var contextFeatures = new FeatureCollection(featureCollection);
            contextFeatures.Set(requestFeature);
            contextFeatures.Set(requestFeature);
            _responseFeature = new ResponseFeature();
            contextFeatures.Set<IHttpResponseFeature>(_responseFeature);
            var requestLifetimeFeature = new HttpRequestLifetimeFeature();
            contextFeatures.Set<IHttpRequestLifetimeFeature>(requestLifetimeFeature);

            _responseStream = new WriteOnlyResponseStream(AbortRequest);
            _responseFeature.Body = _responseStream;
            _responseFeature.StatusCode = 200;
            requestLifetimeFeature.RequestAborted = _requestAbortedSource.Token;

            Context = _factory.Create(contextFeatures);
        }

        public HttpContext Context { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void AbortRequest()
        {
            if (!_pipelineFinished)
            {
                _requestAbortedSource.Cancel();
            }
        }

        /// <summary>
        /// FireOnSendingHeadersAsync is a bit late here, the remaining middlewares are already fully processed, the stream is complete
        /// </summary>
        internal async Task<HttpApplicationMultipart> ResponseTaskAsync()
        {
            await _responseFeature.FireOnSendingHeadersAsync();

            var response = new HttpApplicationMultipart
            (
                _requestFeature.Protocol,
                Context.Response.StatusCode,
                Context.Features.Get<IHttpResponseFeature>().ReasonPhrase,
                _responseStream,
                Context.Features.Get<IHttpResponseFeature>().Headers
            );
            await _responseFeature.FireOnResponseCompletedAsync();
            return response;
        }

        internal void Abort(Exception exception)
        {
            _pipelineFinished = true;
            _responseStream.Abort(exception);
        }

        ~RequestState()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _factory.Dispose(Context);
            }
        }
    }
}