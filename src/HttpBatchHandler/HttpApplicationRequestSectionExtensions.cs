using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HttpBatchHandler
{
    public static class HttpApplicationRequestSectionExtensions
    {
        private const int DefaultBufferSize = 1024 * 4;

        /// <summary>
        ///     TODO: Better ways to parse the content-type?
        /// </summary>
        public static async Task<HttpApplicationRequestSection> ReadNextHttpApplicationRequestSectionAsync(
            this MultipartReader reader, HostString hostString,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var section = await reader.ReadNextSectionAsync(cancellationToken);
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
            var bufferedStream = new BufferedReadStream(section.Body, DefaultBufferSize);
            var requestLineParts = await ReadRequestLineAsync(bufferedStream, cancellationToken);
            if (requestLineParts.Length != 3)
            {
                throw new InvalidDataException("Invalid request line.");
            }
            // Validation of the request line parts necessary?
            var headers = await ReadHeadersAsync(bufferedStream, cancellationToken);
            var uri = BuildUri(hostString, requestLineParts[1]);
            return new HttpApplicationRequestSection
            {
                RequestFeature = new HttpRequestFeature
                {
                    Body = bufferedStream,
                    Headers = new HeaderDictionary(headers),
                    Method = requestLineParts[0],
                    Protocol = requestLineParts[2],
                    Path = uri.AbsolutePath,
                    PathBase = string.Empty,
                    Scheme = uri.Scheme,
                    QueryString = uri.Query
                }
            };
        }

        /// <summary>
        ///     TODO: Is manual query splitting the right way?
        /// </summary>
        private static Uri BuildUri(HostString hostString, string pathAndQuery)
        {
            if (!hostString.HasValue)
            {
                return null;
            }
            var ub = new UriBuilder("http", hostString.Host);
            if (hostString.Port.HasValue)
            {
                ub.Port = hostString.Port.Value;
            }
            var querydelimiter = pathAndQuery.IndexOf('?');
            if (querydelimiter != -1)
            {
                ub.Path = pathAndQuery.Substring(0, querydelimiter);
                ub.Query = pathAndQuery.Substring(querydelimiter + 1);
            }
            else
            {
                ub.Path = pathAndQuery;
            }
            return ub.Uri;
        }


        private static async Task<string[]> ReadRequestLineAsync(BufferedReadStream stream,
            CancellationToken cancellationToken)
        {
            var line = await stream.ReadLineAsync(MultipartReader.DefaultHeadersLengthLimit, cancellationToken);
            return line.Split(' ');
        }

        private static async Task<Dictionary<string, StringValues>> ReadHeadersAsync(BufferedReadStream stream,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var totalSize = 0;
            var accumulator = new KeyValueAccumulator();
            var line = await stream.ReadLineAsync(MultipartReader.DefaultHeadersLengthLimit - totalSize,
                cancellationToken);
            while (!string.IsNullOrEmpty(line))
            {
                if (MultipartReader.DefaultHeadersLengthLimit - totalSize < line.Length)
                {
                    throw new InvalidDataException(
                        $"Multipart headers length limit {MultipartReader.DefaultHeadersLengthLimit} exceeded.");
                }
                totalSize += line.Length;
                var splitIndex = line.IndexOf(':');
                if (splitIndex <= 0)
                {
                    throw new InvalidDataException($"Invalid header line: {line}");
                }

                var name = line.Substring(0, splitIndex);
                var value = line.Substring(splitIndex + 1, line.Length - splitIndex - 1).Trim();
                accumulator.Append(name, value);
                if (accumulator.KeyCount > MultipartReader.DefaultHeadersCountLimit)
                {
                    throw new InvalidDataException(
                        $"Multipart headers count limit {MultipartReader.DefaultHeadersCountLimit} exceeded.");
                }

                line = await stream.ReadLineAsync(MultipartReader.DefaultHeadersLengthLimit - totalSize,
                    cancellationToken);
            }

            return accumulator.GetResults();
        }
    }
}