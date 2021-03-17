using System;
using System.Buffers;

namespace HotChocolate.Utilities
{
    internal sealed class ArrayWriter
        : IBufferWriter<byte>
        , IDisposable
    {
        private const int _initialBufferSize = 512;
        private byte[] _buffer;
        private int _capacity;
        private int _start;
        private bool _disposed;
        
        public ArrayWriter()
        {
            _buffer = ArrayPool<byte>.Shared.Rent(_initialBufferSize);
            _capacity = _buffer.Length;
            _start = 0;
        }

        public int Length => _start;

        public ReadOnlyMemory<byte> Body => _buffer.AsMemory().Slice(0, _start);

        public byte[] GetInternalBuffer() => _buffer;

        public void Advance(int count)
        {
            _start += count;
            _capacity -= count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            var size = sizeHint < 1 ? _initialBufferSize : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.AsMemory().Slice(_start, size);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var size = sizeHint < 1 ? _initialBufferSize : sizeHint;
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
            _start = 0;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = Array.Empty<byte>();
                _disposed = true;
            }
        }
    }
}
