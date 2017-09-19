using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace HttpBatchHandler.Multipart
{
    public static class HttpContentExtensions
    {
        public static async Task<MultipartReader> ReadAsMultipartAsync(this HttpContent content,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!content.Headers.IsMultipart())
            {
                return null;
            }
            var boundary = content.Headers.GetMultipartBoundary();
            if (string.IsNullOrEmpty(boundary))
            {
                return null;
            }
            var stream = await content.ReadAsStreamAsync();
            var reader = new MultipartReader(boundary, stream);
            return reader;
        }

        private static bool IsMultipart(this HttpContentHeaders headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }
            return headers.ContentType.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetMultipartBoundary(this HttpContentHeaders headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }
            if (headers.IsMultipart())
            {
                var boundaryParam = headers.ContentType.Parameters.FirstOrDefault(a =>
                    string.Equals(a.Name, "boundary", StringComparison.OrdinalIgnoreCase));
                return boundaryParam?.Value.Trim('"');
            }
            return null;
        }
    }
}
