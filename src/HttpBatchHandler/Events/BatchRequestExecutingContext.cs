using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler.Events
{
    public class BatchRequestExecutingContext
    {
        public HttpApplicationRequestSection Request { get; set; }
        public FeatureCollection Features { get; set; }
        public object State { get; set; }
    }
}