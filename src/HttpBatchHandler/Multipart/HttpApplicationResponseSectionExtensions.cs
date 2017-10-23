using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace HttpBatchHandler.Multipart
{
    public static class HttpApplicationResponseSectionExtensions
    {
        private static readonly char[] SpaceArray = new []{' '};
        public static async Task<string> ReadAsStringAsync(this HttpApplicationResponseSection section,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (section.ResponseFeature?.Body == null)
            {
                return null;
            }
            return await section.ResponseFeature?.Body.ReadAsStringAsync(cancellationToken);
        }

        public static async Task<HttpApplicationResponseSection> ReadNextHttpApplicationResponseSectionAsync(
            this MultipartReader reader, CancellationToken cancellationToken = default(CancellationToken))
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
                StringComparer.OrdinalIgnoreCase.Equals(a.Value.Value, "response"));
            if (param == null)
            {
                throw new InvalidDataException("Invalid Content-Type.");
            }
            var bufferedStream = new BufferedReadStream(section.Body, SectionHelper.DefaultBufferSize);
            var responseLine = await ReadResponseLineAsync(bufferedStream, cancellationToken);
            if (responseLine.Length != 3)
            {
                throw new InvalidDataException("Invalid request line.");
            }
            var headers = await SectionHelper.ReadHeadersAsync(bufferedStream, cancellationToken);
            return new HttpApplicationResponseSection
            {
                ResponseFeature = new ResponseFeature
                {
                    Body = bufferedStream,
                    Protocol = responseLine[0],
                    StatusCode = int.Parse(responseLine[1]),
                    ReasonPhrase = responseLine[2],
                    Headers = new HeaderDictionary(headers)
                }
            };
        }


        private static async Task<string[]> ReadResponseLineAsync(BufferedReadStream stream,
            CancellationToken cancellationToken)
        {
            var line = await stream.ReadLineAsync(MultipartReader.DefaultHeadersLengthLimit, cancellationToken);
            return line.Split(SpaceArray, 3);
        }
    }
}