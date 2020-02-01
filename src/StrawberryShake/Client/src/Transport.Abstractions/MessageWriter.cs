using System;
using System.Buffers;

namespace StrawberryShake.Transport
{
    public class MessageWriter
        : IRequestWriter
    {
        private const int _initialBufferSize = 1024;
        private const int _defaultMemorySize = 256;
        private const int _minMemorySize = 1;
        private byte[] _buffer;
        private int _capacity;
        private int _start;
        private bool _disposed;

        public MessageWriter()
        {
            _buffer = ArrayPool<byte>.Shared.Rent(_initialBufferSize);
            _capacity = _buffer.Length;
        }

        public int Length => _start;

        public ReadOnlyMemory<byte> Body => _buffer.AsMemory().Slice(0, _start);

        public byte[] GetInternalBuffer() => _buffer;

        public void Advance(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            _start += count;
            _capacity -= count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var size = sizeHint < _minMemorySize ? _defaultMemorySize : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.AsMemory().Slice(_start, size);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var size = sizeHint < _minMemorySize ? _defaultMemorySize : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.AsSpan().Slice(_start, size);
        }

        private void EnsureBufferCapacity(int neededCapacity)
        {
            if (_capacity < neededCapacity)
            {
                byte[] buffer = _buffer;

                int newSize = buffer.Length * 2;
                if (neededCapacity > buffer.Length)
                {
                    newSize += neededCapacity;
                }

                _buffer = ArrayPool<byte>.Shared.Rent(newSize);
                _capacity += _buffer.Length - buffer.Length;

                buffer.AsSpan().CopyTo(_buffer);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public void Clear()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = ArrayPool<byte>.Shared.Rent(_initialBufferSize);
            _capacity = _buffer.Length;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = Array.Empty<byte>();
                _disposed = true;
            }
        }
    }
}
