using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpBatchHandler
{
    internal static class StreamExtensions
    {
        public static async Task<string> ReadAsStringAsync(this Stream stream,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var tr = new StreamReader(stream))
            {
                return await tr.ReadToEndAsync();
            }
        }
    }
}