using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    internal partial class MessagePipeline
        : IMessagePipeline
    {

        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IResultParserResolver _parserResolver;
        private readonly IMessageHandler[] _messageHandlers;

        public MessagePipeline(IEnumerable<IMessageHandler> messageHandlers)
        {
            if (messageHandlers is null)
            {
                throw new ArgumentNullException(nameof(messageHandlers));
            }

            _messageHandlers = messageHandlers.ToArray();
        }

        public async Task ProcessAsync(
            ISocketConnection connection,
            ReadOnlySequence<byte> slice,
            CancellationToken cancellationToken)
        {
            if (TryParseMessage(slice, out OperationMessage? message))
            {
                await HandleMessageAsync(connection, message!, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await connection.SendAsync(
                    KeepConnectionAliveMessage.Default.Serialize(),
                    CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        private bool TryParseMessage(
            ReadOnlySequence<byte> slice,
            out OperationMessage? message)
        {
            ReadOnlySpan<byte> messageData;
            byte[]? buffer = null;

            if (slice.IsSingleSegment)
            {
                messageData = slice.First.Span;
            }
            else
            {
                buffer = ArrayPool<byte>.Shared.Rent(1024 * 4);
                var buffered = 0;

                SequencePosition position = slice.Start;

                while (slice.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                {
                    ReadOnlySpan<byte> span = memory.Span;
                    var bytesRemaining = buffer.Length - buffered;

                    if (span.Length > bytesRemaining)
                    {
                        byte[] next = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
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
                if (messageData.Length == 0
                    || (messageData.Length == 1 && messageData[0] == default))
                {
                    message = null;
                    return false;
                }

                GraphQLSocketMessage parsedMessage =
                    Utf8GraphQLRequestParser.ParseMessage(messageData);
                message = DeserializeMessage(parsedMessage);
                return true;
            }
            catch (SyntaxException)
            {
                message = null;
                return false;
            }
            finally
            {
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private async Task HandleMessageAsync(
            ISocketConnection connection,
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < _messageHandlers.Length; i++)
            {
                IMessageHandler handler = _messageHandlers[i];

                if (handler.CanHandle(message))
                {
                    await handler.HandleAsync(
                            connection,
                            message,
                            cancellationToken)
                        .ConfigureAwait(false);

                    // the message is handled and we are done.
                    return;
                }
            }

            // TODO : resources
            throw new NotSupportedException(
                "The specified message type is not supported.");
        }
    }
}
