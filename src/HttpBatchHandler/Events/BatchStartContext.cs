using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler.Events
{
    public class BatchStartContext
    {
        /// <summary>
        /// The incoming multipart request
        /// </summary>
        public HttpRequest Request { get; set; }
        /// <summary>
        /// State
        /// </summary>
        public object State { get; set; }
    }
}