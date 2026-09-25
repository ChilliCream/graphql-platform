using System.Buffers;

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client;
#else
namespace HotChocolate.Transport.Sockets.Client;
#endif

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
