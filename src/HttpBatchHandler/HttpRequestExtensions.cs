using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HttpBatchHandler
{
    public static class HttpRequestExtensions
    {
        public static bool IsMultiPartBatchRequest(this HttpRequest request)
        {
            return request.ContentType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public static StringSegment ContentTypeBoundary(this HttpRequest request)
        {
            if (MediaTypeHeaderValue.TryParse(request.ContentType, out var parsedValue))
            {
                return HeaderUtilities.RemoveQuotes(parsedValue.Boundary);
            }
            return null;
        }
    }
}