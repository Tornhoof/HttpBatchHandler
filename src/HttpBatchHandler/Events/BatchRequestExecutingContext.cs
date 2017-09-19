using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler.Events
{
    public class BatchRequestExecutingContext
    {
        /// <summary>
        ///     The individual request
        /// </summary>
        public HttpRequest Request { get; set; }

        /// <summary>
        ///     State
        /// </summary>
        public object State { get; set; }
    }
}