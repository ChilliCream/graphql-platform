using System.Buffers;

namespace HotChocolate.Transport.Sockets.Client;

internal static class ArrayWriterExtensions
{
    public static void Write(this IBufferWriter<byte> writer, ReadOnlySequence<byte> sequence)
    {
        foreach (var segment in sequence)
        {
            writer.Write(segment.Span);
        }
    }
}
