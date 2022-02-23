using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using static HotChocolate.AspNetCore.ServerDefaults;
using static HotChocolate.AspNetCore.ThrowHelper;
using static HotChocolate.Language.Utf8GraphQLRequestParser;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

internal static class MessageUtilities
{
    public static bool TryParseMessage(
        ReadOnlySequence<byte> body,
        out GraphQLSocketMessage message,
        [NotNullWhen(true)] out byte[]? rentedBuffer,
        int maxAllowedMessageSize = MaxAllowedRequestSize)
    {
        ReadOnlySpan<byte> messageData;
        byte[]? buffer = null;

        if (body.IsSingleSegment)
        {
            messageData = body.First.Span;
        }
        else
        {
            buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
            var buffered = 0;

            SequencePosition position = body.Start;
            while (body.TryGet(ref position, out ReadOnlyMemory<byte> memory))
            {
                ReadOnlySpan<byte> span = memory.Span;
                var bytesRemaining = buffer.Length - buffered;

                if (buffered > maxAllowedMessageSize)
                {
                    throw DefaultHttpRequestParser_MaxRequestSizeExceeded();
                }

                if (span.Length > bytesRemaining)
                {
                    var next = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Buffer.BlockCopy(buffer, 0, next, 0, buffer.Length);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = next;
                }

                for (var i = 0; i < span.Length; i++)
                {
                    buffer[buffered++] = span[i];
                }
            }

            messageData = buffer;
            messageData = messageData.Slice(0, buffered);
        }

        try
        {
            if (messageData.Length == 0 ||
                (messageData.Length == 1 && messageData[0] == default))
            {
                message = default;
                rentedBuffer = null;
                return false;
            }

            message = ParseMessage(messageData);
            rentedBuffer = buffer!;
            buffer = null;
            return true;
        }
        catch (SyntaxException)
        {
            message = default;
            rentedBuffer = null;
            return false;
        }
        finally
        {
            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
