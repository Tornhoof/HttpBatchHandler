using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpBatchHandler
{
    internal class WriteOnlyResponseStream : Stream
    {
        private readonly Action _abortRequest;
        private readonly Queue<ArraySegment<byte>> _data = new Queue<ArraySegment<byte>>();
        private bool _aborted;
        private Exception _abortException;
        private bool _complete;

        public WriteOnlyResponseStream(Action abortRequest)
        {
            _abortRequest = abortRequest;
        }

        public override bool CanRead { get; } = false;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;

        public override long Length
            => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public void Abort(Exception exception = null)
        {
            _aborted = true;
            _abortException = exception ?? new OperationCanceledException();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback,
            object state)
        {
            throw new NotSupportedException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback,
            object state)
        {
            var task = WriteAsync(buffer, offset, count, default(CancellationToken), state);
            if (callback != null)
            {
                task.ContinueWith(callback.Invoke);
            }
            return task;
        }

        public void Complete()
        {
            _complete = true;
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            while (_data.Count > 0)
            {
                var buffer = _data.Dequeue();
                await destination.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken);
                ArrayPool<byte>.Shared.Return(buffer.Array);
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            ((Task<object>) asyncResult).GetAwaiter().GetResult();
        }

        public override void Flush()
        {
            CheckComplete();
            CheckAborted();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task<int>
            ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, default(CancellationToken)).GetAwaiter().GetResult();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckAborted();
            CheckComplete();
            var data = ArrayPool<byte>.Shared.Rent(count);
            Buffer.BlockCopy(buffer, offset, data, 0, count);
            var segment = new ArraySegment<byte>(data, 0, count);
            _data.Enqueue(segment);
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (_data.Count > 0)
                {
                    var buffer = _data.Dequeue();
                    ArrayPool<byte>.Shared.Return(buffer.Array);
                }
                _abortRequest();
            }
            base.Dispose(disposing);
        }

        private void CheckAborted()
        {
            if (_aborted)
            {
                throw new IOException("Aborted", _abortException);
            }
        }

        private void CheckComplete()
        {
            if (_complete)
            {
                throw new IOException("Completed");
            }
        }

        private Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, object state)
        {
            var tcs = new TaskCompletionSource<object>(state);
            var task = WriteAsync(buffer, offset, count, cancellationToken);
            task.ContinueWith((task2, state2) =>
            {
                var tcs2 = (TaskCompletionSource<object>) state2;
                if (task2.IsCanceled)
                {
                    tcs2.SetCanceled();
                }
                else if (task2.IsFaulted)
                {
                    tcs2.SetException(task2.Exception);
                }
                else
                {
                    tcs2.SetResult(null);
                }
            }, tcs, cancellationToken);
            return tcs.Task;
        }
    }
}