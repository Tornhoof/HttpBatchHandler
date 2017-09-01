using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

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

            var boundary = httpContext.Request.ContentTypeBoundary();
            if (boundary == null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Invalid Content-Type.");
                return;
            }
            var reader = new MultipartReader(boundary.Value, httpContext.Request.Body);
            // MultiPartContent should probably be replaced with something which can directly write into the response.Body
            using (var result = new MultipartContent("batch", "batch_" + Guid.NewGuid()))
            {
                HttpApplicationRequestSection section;
                while ((section = await reader.ReadNextHttpApplicationRequestSectionAsync(httpContext.Request.Host)) !=
                       null)
                {
                    var state = new RequestState(section.RequestFeature, _factory, httpContext.RequestServices);
                    var registration = httpContext.RequestAborted.Register(state.AbortRequest);

                    // Async offload, don't let the test code block the caller.
                    var offload = Task.Factory.StartNew(async () =>
                    {
                        try
                        {
                            await _next.Invoke(state.Context);
                            await state.CompleteResponseAsync();
                        }
                        catch (Exception ex)
                        {
                            state.Abort(ex);
                        }
                        finally
                        {
                            state.ServerCleanup();
                            registration.Dispose();
                        }
                    });
                    var response = await state.ResponseTask;
                    result.Add(response);
                }
                foreach (var httpContentHeader in result.Headers)
                {
                    foreach (var value in httpContentHeader.Value)
                    {
                        httpContext.Response.Headers.Add(httpContentHeader.Key, value);
                    }
                }
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                await result.CopyToAsync(httpContext.Response.Body);
                foreach (var response in result)
                {
                    response.Dispose();
                }
            }
        }
    }
}