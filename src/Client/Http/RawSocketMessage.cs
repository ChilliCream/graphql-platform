using System;
using System.Buffers;

namespace StrawberryShake.Http
{
    internal sealed class RawSocketMessage
       : IBufferWriter<byte>
       , IDisposable
    {
        private IMemoryOwner<byte> _buffer;
        private int _capacity;
        private int _start = 0;
        private bool _disposed;

        public RawSocketMessage()
        {
            _buffer = MemoryPool<byte>.Shared.Rent();
            _capacity = _buffer.Memory.Length;
        }

        public ReadOnlyMemory<byte> Body => _buffer.Memory.Slice(0, _start);

        public void Advance(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            _start += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            int size = sizeHint < 1 ? 256 : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.Memory.Slice(_start, size);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            int size = sizeHint < 1 ? 256 : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.Memory.Span.Slice(_start, size);
        }

        private void EnsureBufferCapacity(int size)
        {
            if (_capacity < size)
            {
                IMemoryOwner<byte> current = _buffer;
                _buffer = MemoryPool<byte>.Shared.Rent(current.Memory.Length * 2);
                _capacity += current.Memory.Length;
                current.Memory.CopyTo(_buffer.Memory);
                current.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _buffer.Dispose();
                _disposed = true;
            }
        }
    }
}
