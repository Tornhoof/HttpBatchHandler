using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HttpBatchHandler.Multipart
{
    public static class HttpApplicationRequestSectionExtensions
    {
        public static async Task<HttpApplicationRequestSection> ReadNextHttpApplicationRequestSectionAsync(
            this MultipartReader reader, PathString pathBase = default, CancellationToken cancellationToken = default)
        {
            var section = await reader.ReadNextSectionAsync(cancellationToken).ConfigureAwait(false);
            if (section == null)
            {
                return null; // if null we're done
            }

            var contentTypeHeader = MediaTypeHeaderValue.Parse(section.ContentType);
            if (!contentTypeHeader.MediaType.HasValue ||
                !StringComparer.OrdinalIgnoreCase.Equals(contentTypeHeader.MediaType.Value, "application/http"))
            {
                throw new InvalidDataException("Invalid Content-Type.");
            }
            var param = contentTypeHeader.Parameters.SingleOrDefault(a =>
                a.Name.HasValue && a.Value.HasValue &&
                StringComparer.OrdinalIgnoreCase.Equals(a.Name.Value, "msgtype") &&
                StringComparer.OrdinalIgnoreCase.Equals(a.Value.Value, "request"));
            if (param == null)
            {
                throw new InvalidDataException("Invalid Content-Type.");
            }
            var bufferedStream = new BufferedReadStream(section.Body, SectionHelper.DefaultBufferSize);
            var requestLineParts = await ReadRequestLineAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
            if (requestLineParts.Length != 3)
            {
                throw new InvalidDataException("Invalid request line.");
            }
            // Validation of the request line parts necessary?
            var headers = await SectionHelper.ReadHeadersAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
            if (!headers.TryGetValue(HeaderNames.Host, out var hostHeader))
            {
                throw new InvalidDataException("No Host Header");
            }
            var uri = BuildUri(hostHeader, requestLineParts[1]);
            var fullPath = PathString.FromUriComponent(uri);
            var feature = new HttpRequestFeature
            {
                Body = bufferedStream,
                Headers = new HeaderDictionary(headers),
                Method = requestLineParts[0],
                Protocol = requestLineParts[2],
                Scheme = uri.Scheme,
                QueryString = uri.Query
            };
            if (fullPath.StartsWithSegments(pathBase, out var remainder))
            {
                feature.PathBase = pathBase.Value;
                feature.Path = remainder.Value;
            }
            else
            {
                feature.PathBase = string.Empty;
                feature.Path = fullPath.Value;
            }
            return new HttpApplicationRequestSection
            {
                RequestFeature = feature
            };
        }

        private static Uri BuildUri(StringValues hostHeader, string pathAndQuery)
        {
            if (hostHeader.Count != 1)
            {
                throw new InvalidOperationException("Invalid Host Header");
            }
            var hostString = new HostString(hostHeader.Single());
            if (!hostString.HasValue)
            {
                return null;
            }
            var fullUri = $"http://{hostString.ToUriComponent()}{pathAndQuery}";
            var uri = new Uri(fullUri);
            return uri;
        }


        private static async Task<string[]> ReadRequestLineAsync(BufferedReadStream stream,
            CancellationToken cancellationToken)
        {
            var line = await stream.ReadLineAsync(MultipartReader.DefaultHeadersLengthLimit, cancellationToken).ConfigureAwait(false);
            return line.Split(' ');
        }
    }
}