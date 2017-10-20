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
        private readonly ResponseFeature _responseFeature;
        private readonly WriteOnlyResponseStream _responseStream;
        private bool _pipelineFinished;

        internal RequestState(IHttpRequestFeature requestFeature, IHttpContextFactory factory,
            IFeatureCollection featureCollection)
        {
            _factory = factory;
            _requestAbortedSource = new CancellationTokenSource();
            _pipelineFinished = false;

            var contextFeatures = new FeatureCollection(featureCollection);
            contextFeatures.Set(requestFeature);
            _responseFeature = new ResponseFeature {Protocol = requestFeature.Protocol};
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _factory.Dispose(Context);
            }
        }

        internal void Abort(Exception exception)
        {
            _pipelineFinished = true;
            _responseStream.Abort(exception);
        }

        internal void AbortRequest()
        {
            if (!_pipelineFinished)
            {
                _requestAbortedSource.Cancel();
            }
        }

        /// <summary>
        ///     FireOnSendingHeadersAsync is a bit late here, the remaining middlewares are already fully processed, the testhost
        ///     does it on the first body stream write, which is more logical
        ///     but I'm not certain about the added complexity
        /// </summary>
        internal async Task<ResponseFeature> ResponseTaskAsync()
        {
            _pipelineFinished = true;
            await _responseFeature.FireOnSendingHeadersAsync();
            await _responseFeature.FireOnResponseCompletedAsync();
            _responseStream.Complete();
            return _responseFeature;
        }

        ~RequestState()
        {
            Dispose(false);
        }
    }
}