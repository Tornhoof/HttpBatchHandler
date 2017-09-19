using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler.Multipart
{
    public class HttpApplicationResponseSection
    {
        public IHttpResponseFeature ResponseFeature { get; set; }
    }
}