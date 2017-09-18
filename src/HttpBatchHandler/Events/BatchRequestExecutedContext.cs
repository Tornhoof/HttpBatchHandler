using System;
using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler.Events
{
    public class BatchRequestExecutedContext
    {
        public IHttpRequestFeature Request { get; set; }
        public IHttpResponseFeature Response { get; set; }
        public bool Abort { get; set; }
        public Exception Exception { get; set; }
        public object State { get; set; }
    }
}