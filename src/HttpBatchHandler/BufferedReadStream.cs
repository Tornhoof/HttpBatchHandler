// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpBatchHandler
{
    /// <summary>
    /// This is copied directly from 
    /// https://github.com/aspnet/HttpAbstractions/blob/master/src/Microsoft.AspNetCore.WebUtilities/BufferedReadStream.cs
    /// </summary>
    internal class BufferedReadStream : Stream
    {
        private const byte Cr = (byte) '\r';
        private const byte Lf = (byte) '\n';
        private readonly byte[] _buffer;
        private readonly ArrayPool<byte> _bytePool;

        private readonly Stream _inner;
        private int _bufferCount;
        private int _bufferOffset;
        private bool _disposed;

        public BufferedReadStream(Stream inner, int bufferSize)
            : this(inner, bufferSize, ArrayPool<byte>.Shared)
        {
        }

        public BufferedReadStream(Stream inner, int bufferSize, ArrayPool<byte> bytePool)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _bytePool = bytePool;
            _buffer = bytePool.Rent(bufferSize);
        }

        public ArraySegment<byte> BufferedData => new ArraySegment<byte>(_buffer, _bufferOffset, _bufferCount);

        public override bool CanRead => _inner.CanRead || _bufferCount > 0;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanTimeout => _inner.CanTimeout;

        public override bool CanWrite => _inner.CanWrite;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position - _bufferCount;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Position must be positive.");
                }
                if (value == Position)
                {
                    return;
                }

                // Backwards?
                if (value <= _inner.Position)
                {
                    // Forward within the buffer?
                    var innerOffset = (int) (_inner.Position - value);
                    if (innerOffset <= _bufferCount)
                    {
                        // Yes, just skip some of the buffered data
                        _bufferOffset += innerOffset;
                        _bufferCount -= innerOffset;
                    }
                    else
                    {
                        // No, reset the buffer
                        _bufferOffset = 0;
                        _bufferCount = 0;
                        _inner.Position = value;
                    }
                }
                else
                {
                    // Forward, reset the buffer
                    _bufferOffset = 0;
                    _bufferCount = 0;
                    _inner.Position = value;
                }
            }
        }

        public bool EnsureBuffered()
        {
            if (_bufferCount > 0)
            {
                return true;
            }
            // Downshift to make room
            _bufferOffset = 0;
            _bufferCount = _inner.Read(_buffer, 0, _buffer.Length);
            return _bufferCount > 0;
        }

        public bool EnsureBuffered(int minCount)
        {
            if (minCount > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(minCount), minCount,
                    "The value must be smaller than the buffer size: " + _buffer.Length);
            }
            while (_bufferCount < minCount)
            {
                // Downshift to make room
                if (_bufferOffset > 0)
                {
                    if (_bufferCount > 0)
                    {
                        Buffer.BlockCopy(_buffer, _bufferOffset, _buffer, 0, _bufferCount);
                    }
                    _bufferOffset = 0;
                }
                var read = _inner.Read(_buffer, _bufferOffset + _bufferCount,
                    _buffer.Length - _bufferCount - _bufferOffset);
                _bufferCount += read;
                if (read == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> EnsureBufferedAsync(CancellationToken cancellationToken)
        {
            if (_bufferCount > 0)
            {
                return true;
            }
            // Downshift to make room
            _bufferOffset = 0;
            _bufferCount = await _inner.ReadAsync(_buffer, 0, _buffer.Length, cancellationToken);
            return _bufferCount > 0;
        }

        public async Task<bool> EnsureBufferedAsync(int minCount, CancellationToken cancellationToken)
        {
            if (minCount > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(minCount), minCount,
                    "The value must be smaller than the buffer size: " + _buffer.Length);
            }
            while (_bufferCount < minCount)
            {
                // Downshift to make room
                if (_bufferOffset > 0)
                {
                    if (_bufferCount > 0)
                    {
                        Buffer.BlockCopy(_buffer, _bufferOffset, _buffer, 0, _bufferCount);
                    }
                    _bufferOffset = 0;
                }
                var read = await _inner.ReadAsync(_buffer, _bufferOffset + _bufferCount,
                    _buffer.Length - _bufferCount - _bufferOffset, cancellationToken);
                _bufferCount += read;
                if (read == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _inner.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBuffer(buffer, offset, count);

            // Drain buffer
            if (_bufferCount > 0)
            {
                var toCopy = Math.Min(_bufferCount, count);
                Buffer.BlockCopy(_buffer, _bufferOffset, buffer, offset, toCopy);
                _bufferOffset += toCopy;
                _bufferCount -= toCopy;
                return toCopy;
            }

            return _inner.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            ValidateBuffer(buffer, offset, count);

            // Drain buffer
            if (_bufferCount > 0)
            {
                var toCopy = Math.Min(_bufferCount, count);
                Buffer.BlockCopy(_buffer, _bufferOffset, buffer, offset, toCopy);
                _bufferOffset += toCopy;
                _bufferCount -= toCopy;
                return toCopy;
            }

            return await _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public string ReadLine(int lengthLimit)
        {
            CheckDisposed();
            using (var builder = new MemoryStream(200))
            {
                bool foundCr = false, foundCrlf = false;

                while (!foundCrlf && EnsureBuffered())
                {
                    if (builder.Length > lengthLimit)
                    {
                        throw new InvalidDataException($"Line length limit {lengthLimit} exceeded.");
                    }
                    ProcessLineChar(builder, ref foundCr, ref foundCrlf);
                }

                return DecodeLine(builder, foundCrlf);
            }
        }

        public async Task<string> ReadLineAsync(int lengthLimit, CancellationToken cancellationToken)
        {
            CheckDisposed();
            using (var builder = new MemoryStream(200))
            {
                bool foundCr = false, foundCrlf = false;

                while (!foundCrlf && await EnsureBufferedAsync(cancellationToken))
                {
                    if (builder.Length > lengthLimit)
                    {
                        throw new InvalidDataException($"Line length limit {lengthLimit} exceeded.");
                    }

                    ProcessLineChar(builder, ref foundCr, ref foundCrlf);
                }

                return DecodeLine(builder, foundCrlf);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                Position = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                Position = Position + offset;
            }
            else // if (origin == SeekOrigin.End)
            {
                Position = Length + offset;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                _bytePool.Return(_buffer);

                if (disposing)
                {
                    _inner.Dispose();
                }
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BufferedReadStream));
            }
        }

        private string DecodeLine(MemoryStream builder, bool foundCrlf)
        {
            // Drop the final CRLF, if any
            var length = foundCrlf ? builder.Length - 2 : builder.Length;
            return Encoding.UTF8.GetString(builder.ToArray(), 0, (int) length);
        }

        private void ProcessLineChar(MemoryStream builder, ref bool foundCr, ref bool foundCrlf)
        {
            var b = _buffer[_bufferOffset];
            builder.WriteByte(b);
            _bufferOffset++;
            _bufferCount--;
            if (b == Cr)
            {
                foundCr = true;
            }
            else if (b == Lf)
            {
                if (foundCr)
                {
                    foundCrlf = true;
                }
                else
                {
                    foundCr = false;
                }
            }
        }

        private void ValidateBuffer(byte[] buffer, int offset, int count)
        {
            // Delegate most of our validation.
            var ignored = new ArraySegment<byte>(buffer, offset, count);
            if (count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "The value must be greater than zero.");
            }
        }
    }
}