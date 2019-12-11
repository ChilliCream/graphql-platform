using System;
using System.Buffers;
using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Stitching.Utilities
{
    internal sealed class RequestWriter
        : IBufferWriter<byte>
        , IDisposable
    {
        private static readonly JsonWriterOptions _options =
            new JsonWriterOptions { SkipValidation = true };
        private readonly Utf8JsonWriter _writer;
        private byte[] _buffer;
        private int _capacity;
        private int _start;
        private bool _disposed;

        public RequestWriter()
        {
            _buffer = ArrayPool<byte>.Shared.Rent(1024);
            _capacity = _buffer.Length;
            _writer = new Utf8JsonWriter(this, _options);
        }

        public int Length => _start;

        public ReadOnlyMemory<byte> Body => _buffer.AsMemory().Slice(0, _start);

        public byte[] GetInternalBuffer() => _buffer;

        public void WriteStartObject()
        {
            _writer.WriteStartObject();
            _writer.Flush();
        }

        public void WriteEndObject()
        {
            _writer.WriteEndObject();
            _writer.Flush();
        }

        public void WriteStartArray()
        {
            _writer.WriteStartArray();
            _writer.Flush();
        }

        public void WriteEndArray()
        {
            _writer.WriteEndArray();
            _writer.Flush();
        }

        public void WriteQuery(IQuery query)
        {
            _writer.WriteString("query", query.ToSpan());
            _writer.Flush();
        }

        public void WriteOperationName(string operationName)
        {
            if (operationName is { })
            {
                _writer.WriteString("operationName", operationName);
                _writer.Flush();
            }
        }

        public void WritePropertyName(string name)
        {
            _writer.WritePropertyName(name);
            _writer.Flush();
        }

        public void WriteNullValue()
        {
            _writer.WriteNullValue();
            _writer.Flush();
        }

        public void WriteStringValue(string value)
        {
            _writer.WriteStringValue(value);
            _writer.Flush();
        }

        public void WriteBooleanValue(bool value)
        {
            _writer.WriteBooleanValue(value);
            _writer.Flush();
        }

        public void WriteNumberValue(string value)
        {
            Span<byte> span = GetSpan(value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                span[i] = (byte)value[i];
            }

            Advance(value.Length);
        }

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
            var size = sizeHint < 1 ? 256 : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.AsMemory().Slice(_start, size);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            var size = sizeHint < 1 ? 256 : sizeHint;
            EnsureBufferCapacity(size);
            return _buffer.AsSpan().Slice(_start, size);
        }

        private void EnsureBufferCapacity(int size)
        {
            if (_capacity < size)
            {
                byte[] buffer = _buffer;
                _buffer = ArrayPool<byte>.Shared.Rent(size + _buffer.Length);
                _capacity += _buffer.Length;
                buffer.AsSpan().CopyTo(_buffer);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public void Clear()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = ArrayPool<byte>.Shared.Rent(1024);
            _capacity = _buffer.Length;
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
