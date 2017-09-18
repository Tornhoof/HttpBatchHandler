using Microsoft.AspNetCore.Http.Features;

namespace HttpBatchHandler
{
    public static class HttpResponseFeatureExtensions
    {
        
        public static bool IsSuccessStatusCode(this IHttpResponseFeature response) => response.StatusCode >= 200 && response.StatusCode <= 299;
    }
}