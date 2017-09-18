using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler.Events
{
    public class BatchMiddlewareOptions
    {
        public PathString Match { get; set; } = "/api/batch";
        public BatchMiddlewareEvents Events { get; set; } = new BatchMiddlewareEvents();
    }
}
