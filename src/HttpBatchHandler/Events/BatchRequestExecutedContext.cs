using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler.Events
{
    public class BatchRequestExecutedContext
    {
        /// <summary>
        /// The individual HttpRequest
        /// </summary>
        public HttpRequest Request { get; set; }
        /// <summary>
        /// The individual HttpResponse
        /// </summary>
        public HttpResponse Response { get; set; }
        /// <summary>
        /// Abort after this request?
        /// </summary>
        public bool Abort { get; set; }
        /// <summary>
        /// Exception
        /// </summary>
        public Exception Exception { get; set; }
        /// <summary>
        /// State
        /// </summary>
        public object State { get; set; }
    }
}