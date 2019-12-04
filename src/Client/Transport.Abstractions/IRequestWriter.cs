using System;
using System.Buffers;

namespace StrawberryShake.Transport
{
    public interface IRequestWriter
        : IBufferWriter<byte>
        , IDisposable
    {
        int Length { get; }

        ReadOnlyMemory<byte> Body { get; }

        byte[] GetInternalBuffer();

        void Clear();
    }
}
