using System;
using Microsoft.AspNetCore.Http;

namespace HttpBatchHandler
{
    public static class HttpRequestExtensions
    {
        public static bool IsMultiPartBatchRequest(this HttpRequest request)
        {
            return request.ContentType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}