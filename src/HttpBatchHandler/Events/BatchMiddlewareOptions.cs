using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler.Events
{
    public class BatchMiddlewareOptions
    {
        /// <summary>
        ///  Events
        /// </summary>
        public BatchMiddlewareEvents Events { get; set; } = new BatchMiddlewareEvents();
        /// <summary>
        /// Endpoint
        /// </summary>
        public PathString Match { get; set; } = "/api/batch";
    }
}