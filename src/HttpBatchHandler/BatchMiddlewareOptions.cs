using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    public class BatchMiddlewareOptions
    {
        public PathString Match { get; set; } = "/api/batch";
        public BatchMiddlewareEvents Events { get; set; } = new BatchMiddlewareEvents();
    }

    public class BatchMiddlewareEvents
    {
        /// <summary>
        /// Before any request in a batch are executed
        /// </summary>
        public Func<BatchStartContext, Task> OnBatchStart { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Before an individual request in a batch is executed
        /// </summary>
        public Func<BatchRequestExecutingContext, Task> OnBatchRequestExecuting { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// After an individual request in a batch is executed
        /// </summary>
        public Func<BatchRequestExecutedContext, Task> OnBatchRequestExecuted { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// After all batch request are executed
        /// </summary>
        public Func<BatchEndContext, Task> OnBatchEnd { get; set; } = context => Task.FromResult(0);

        /// <summary>
        /// Before any request in a batch are executed
        /// </summary>
        public virtual Task BatchStart(BatchStartContext context) => OnBatchStart(context);

        /// <summary>
        /// Before an individual request in a batch is executed
        /// </summary>
        public virtual Task BatchRequestExecuting(BatchRequestExecutingContext context) => OnBatchRequestExecuting(context);

        /// <summary>
        /// After an individual request in a batch is executed
        /// </summary>
        public virtual Task BatchRequestExecuted(BatchRequestExecutedContext context) => OnBatchRequestExecuted(context);

        /// <summary>
        /// After all batch request are executed
        /// </summary>
        public virtual Task BatchEnd(BatchEndContext context) => OnBatchEnd(context);
    }

    public class BatchEndContext
    {
        public MultipartWriter Writer { get; set; }
        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public bool WriteBody { get; set; } = true;
        public Exception Exception { get; set; }
        public object State { get; set; }
    }

    public class BatchRequestExecutingContext
    {
        public HttpApplicationRequestSection Request { get; set; }
        public FeatureCollection Features { get; set; }
        public object State { get; set; }
    }

    public class BatchStartContext
    {
        public object State { get; set; }
    }

    public class BatchRequestExecutedContext
    {
        public HttpApplicationRequestSection Request { get; set; }
        public HttpApplicationMultipart Response { get; set; }
        public bool Abort { get; set; }
        public Exception Exception { get; set; }
        public object State { get; set; }
    }
}
