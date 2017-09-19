using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler.Multipart
{
    public class HttpApplicationRequestSection
    {
        public IHttpRequestFeature RequestFeature { get; set; }
    }
}