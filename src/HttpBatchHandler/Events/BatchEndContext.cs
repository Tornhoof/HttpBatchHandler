using System;
using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler.Events
{
    public class BatchEndContext
    {
        /// <summary>
        ///     Possible exception
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        ///     If not all requests were executed
        /// </summary>
        public bool IsAborted { get; set; }

        /// <summary>
        ///     If true, then you need to populate the response yourself
        /// </summary>
        public bool IsHandled { get; set; } = false;

        /// <summary>
        ///     The outgoing multipart response
        /// </summary>
        public HttpResponse Response { get; set; }

        /// <summary>
        ///     State
        /// </summary>
        public object State { get; set; }
    }
}