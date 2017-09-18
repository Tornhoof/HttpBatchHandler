using System;
using System.Threading.Tasks;

namespace HttpBatchHandler.Events
{
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
}