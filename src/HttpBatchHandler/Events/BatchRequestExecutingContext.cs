using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler.Events
{
    public class BatchRequestExecutingContext
    {
        public IHttpRequestFeature Request { get; set; }
        public IFeatureCollection Features { get; set; }
        public object State { get; set; }
    }
}