using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler
{
    internal static class HttpResponseExtensions
    {
        public static bool IsSuccessStatusCode(this HttpResponse response)
        {
            return response.StatusCode >= 200 && response.StatusCode <= 299;
        }
    }
}