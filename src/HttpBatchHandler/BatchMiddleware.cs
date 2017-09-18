using System;
using System.Threading.Tasks;
using HttpBatchHandler.Events;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace HttpBatchHandler
{
    public class BatchMiddleware
    {
        private readonly IHttpContextFactory _factory;
        private readonly BatchMiddlewareOptions _options;
        private readonly RequestDelegate _next;

        public BatchMiddleware(RequestDelegate next, IHttpContextFactory factory, BatchMiddlewareOptions options)
        {
            _next = next;
            _factory = factory;
            _options = options;
        }


        public Task Invoke(HttpContext httpContext)
        {
            if (!httpContext.Request.Path.Equals(_options.Match))
            {
                return _next.Invoke(httpContext);
            }
            return InvokeBatchAsync(httpContext);
        }

        /// <summary>
        ///     Is the Async Offload stuff and the rather complicated state and stream handling really necessary?
        /// </summary>
        private async Task InvokeBatchAsync(HttpContext httpContext)
        {
            if (!httpContext.Request.IsMultiPartBatchRequest())
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Invalid Content-Type.").ConfigureAwait(false);
                return;
            }

            var boundary = httpContext.Request.GetMultipartBoundary();
            if (string.IsNullOrEmpty(boundary))
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync("Invalid boundary in Content-Type.").ConfigureAwait(false);
                return;
            }
            var startContext = new BatchStartContext
            {
                Request = httpContext.Request
            };
            await _options.Events.BatchStart(startContext);
            Exception exception = null;
            bool abort = false;
            var cancellationToken = httpContext.RequestAborted;

            var reader = new MultipartReader(boundary, httpContext.Request.Body);
            using (var writer = new MultipartWriter("batch", Guid.NewGuid().ToString()))
            {
                try
                {
                    HttpApplicationRequestSection section;
                    while ((section = await reader.ReadNextHttpApplicationRequestSectionAsync(cancellationToken)) !=
                           null)
                    {
                        httpContext.RequestAborted.ThrowIfCancellationRequested();
                        var preparationContext = new BatchRequestPreparationContext
                        {
                            RequestFeature = section.RequestFeature,
                            Features = CreateDefaultFeatures(httpContext.Features),
                            State = startContext.State,
                        };
                        await _options.Events.BatchRequestPreparation(preparationContext);
                        using (var state =
                            new RequestState(section.RequestFeature, _factory, preparationContext.Features))
                        {
                            using (httpContext.RequestAborted.Register(state.AbortRequest))
                            {
                                var executedContext = new BatchRequestExecutedContext
                                {
                                    Request = state.Context.Request,
                                    State = startContext.State,
                                };
                                try
                                {
                                    var executingContext = new BatchRequestExecutingContext
                                    {
                                        Request = state.Context.Request,
                                        State = startContext.State,
                                    };
                                    await _options.Events.BatchRequestExecuting(executingContext);
                                    await _next.Invoke(state.Context);
                                    var response = await state.ResponseTaskAsync();
                                    executedContext.Response = state.Context.Response;
                                    writer.Add(new HttpApplicationMultipart(response));
                                }
                                catch (Exception ex)
                                {
                                    state.Abort(ex);
                                    executedContext.Exception = ex;
                                }
                                finally
                                {
                                    await _options.Events.BatchRequestExecuted(executedContext);
                                    abort = executedContext.Abort;
                                }
                                if (abort)
                                {
                                    break;
                                }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    var endContext = new BatchEndContext
                    {
                        Exception = exception,
                        State = startContext.State,
                        IsAborted = abort,
                        Response = httpContext.Response,
                    };
                    if (endContext.Exception != null)
                    {
                        endContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    }
                    await _options.Events.BatchEnd(endContext);
                    if (!endContext.IsHandled)
                    {
                        httpContext.Response.Headers.Add(HeaderNames.ContentType, writer.ContentType);
                        await writer.CopyToAsync(httpContext.Response.Body, cancellationToken);
                    }
                }
            }
        }

        private FeatureCollection CreateDefaultFeatures(IFeatureCollection input)
        {
            var output = new FeatureCollection();
            output.Set(input.Get<IServiceProvidersFeature>());
            output.Set(input.Get<IHttpRequestIdentifierFeature>());
            output.Set(input.Get<IAuthenticationFeature>());
            output.Set(input.Get<IHttpAuthenticationFeature>());
            output.Set<IItemsFeature>(new ItemsFeature()); // per request?
            return output;
        }
    }
}