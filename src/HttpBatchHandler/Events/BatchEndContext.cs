using System;
using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler.Events
{
    public class BatchEndContext
    {
        public MultipartWriter Writer { get; set; }
        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public bool WriteBody { get; set; } = true;
        public Exception Exception { get; set; }
        public object State { get; set; }
    }
}