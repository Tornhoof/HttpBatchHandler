using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpBatchHandler
{
    internal interface IMultipart : IDisposable
    {
        Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken));
    }
}