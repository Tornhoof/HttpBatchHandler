using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    public class HttpApplicationRequestSection
    {
        internal HttpRequestFeature RequestFeature { get; set; }
    }
}