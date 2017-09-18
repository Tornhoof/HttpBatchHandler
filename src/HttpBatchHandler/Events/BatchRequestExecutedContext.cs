using System;

namespace HttpBatchHandler.Events
{
    public class BatchRequestExecutedContext
    {
        public HttpApplicationRequestSection Request { get; set; }
        public HttpApplicationMultipart Response { get; set; }
        public bool Abort { get; set; }
        public Exception Exception { get; set; }
        public object State { get; set; }
    }
}