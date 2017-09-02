using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace HttpBatchHandler
{
    public class BatchMiddleware
    {
        private readonly IHttpContextFactory _factory;
        private readonly PathString _match;
        private readonly RequestDelegate _next;

        public BatchMiddleware(RequestDelegate next, IHttpContextFactory factory, PathString match)
        {
            _next = next;
            _factory = factory;
            _match = match;
        }

        /// <summary>
        ///     TODO: Right way to call this middleware each and every time?
        /// </summary>
        public Task Invoke(HttpContext httpContext)
        {
            if (!httpContext.Request.Path.Equals(_match))
            {
                return _next.Invoke(httpContext);
            }
            return InvokeBatchAsync(httpContext);
        }

        /// <summary>
        ///     TODO: Customization (OnXYZCompleted/Started etc.)
        ///     Is the Async Offload stuff and the rather complicated state and stream handling really necessary?
        /// </summary>
        private async Task InvokeBatchAsync(HttpContext httpContext)
        {
            if (!httpContext.Request.IsMultiPartBatchRequest())
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Invalid Content-Type.");
                return;
            }

            var boundary = httpContext.Request.GetMultipartBoundary();
            if (boundary == null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Invalid Content-Type.");
                return;
            }
            var cancellationToken = httpContext.RequestAborted;
            var reader = new MultipartReader(boundary, httpContext.Request.Body);
            // MultiPartContent should probably be replaced with something which can directly write into the response.Body
            var writer = new MultipartWriter("batch", "batch_" + Guid.NewGuid(), httpContext.Response.Body);
            HttpApplicationRequestSection section;
            httpContext.Response.Headers.Add(HeaderNames.ContentType, writer.ContentType);
            while ((section = await reader.ReadNextHttpApplicationRequestSectionAsync(httpContext.Request.Host, cancellationToken)) != null)
            {
                if (httpContext.RequestAborted.IsCancellationRequested)
                {
                    break;
                }
                using (var state = new RequestState(section.RequestFeature, _factory, httpContext.RequestServices))
                {
                    using (httpContext.RequestAborted.Register(state.AbortRequest))
                    {
                        try
                        {
                            await _next.Invoke(state.Context);
                            using (var content = await state.ResponseTaskAsync())
                            {
                                await writer.WritePartAsync(content, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            state.Abort(ex);
                        }
                    }
                }
            }
            //httpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
    }
}