using System.IO.Pipelines;

namespace HotChocolate.Transport.Formatters;

internal static class StreamExtensions
{
    private static readonly StreamPipeWriterOptions s_options = new(
        pool: System.Buffers.MemoryPool<byte>.Shared,
        minimumBufferSize: 4096,
        leaveOpen: true);

    public static PipeWriter CreatePipeWriter(this Stream stream)
        => PipeWriter.Create(stream, s_options);
}
