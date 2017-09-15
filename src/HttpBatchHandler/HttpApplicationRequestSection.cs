using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    public class HttpApplicationRequestSection
    {
        public HttpRequestFeature RequestFeature { get; set; }
    }
}