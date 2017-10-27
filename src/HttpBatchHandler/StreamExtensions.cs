using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpBatchHandler
{
    internal static class StreamExtensions
    {
        public static async Task<string> ReadAsStringAsync(this Stream stream,
            CancellationToken cancellationToken = default)
        {
            using (var tr = new StreamReader(stream))
            {
                return await tr.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}