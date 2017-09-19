using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace HttpBatchHandler.Multipart
{
    internal static class SectionHelper
    {
        public const int DefaultBufferSize = 1024 * 4;

        public static async Task<Dictionary<string, StringValues>> ReadHeadersAsync(BufferedReadStream stream,
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