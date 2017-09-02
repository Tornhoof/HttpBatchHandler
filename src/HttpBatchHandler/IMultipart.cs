using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpBatchHandler
{
    public interface IMultipart
    {
        Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken));
    }
}