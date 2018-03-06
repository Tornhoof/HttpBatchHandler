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
        private static readonly char[] SpaceArray = {' '};

        public static async Task<HttpApplicationResponseSection> ReadNextHttpApplicationResponseSectionAsync(
            this MultipartReader reader, CancellationToken cancellationToken = default)
        {
            var section = await reader.ReadNextSectionAsync(cancellationToken).ConfigureAwait(false);
            if (section == null)
            {
                return null; // if null we're done
            }

            var contentTypeHeader = MediaTypeHeaderValue.Parse(section.ContentType);
            if (!contentTypeHeader.MediaType.HasValue ||
                !contentTypeHeader.MediaType.Equals("application/http", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Invalid Content-Type.");
            }

            var param = contentTypeHeader.Parameters.SingleOrDefault(a =>
                a.Name.HasValue && a.Value.HasValue &&
                a.Name.Value.Equals("msgtype", StringComparison.OrdinalIgnoreCase) &&
                a.Value.Equals("response", StringComparison.OrdinalIgnoreCase));
            if (param == null)
            {
                throw new InvalidDataException("Invalid Content-Type.");
            }

            var bufferedStream = new BufferedReadStream(section.Body, SectionHelper.DefaultBufferSize);
            var responseLine = await ReadResponseLineAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
            if (responseLine.Length != 3)
            {
                throw new InvalidDataException("Invalid request line.");
            }

            var headers = await SectionHelper.ReadHeadersAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
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
            var line = await stream.ReadLineAsync(MultipartReader.DefaultHeadersLengthLimit, cancellationToken)
                .ConfigureAwait(false);
            return line.Split(SpaceArray, 3);
        }
    }
}