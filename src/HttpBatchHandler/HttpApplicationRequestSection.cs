using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    internal class HttpApplicationRequestSection
    {
        public HttpRequestFeature RequestFeature { get; set; }
    }
}