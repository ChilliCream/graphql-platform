using System;
using System.Buffers;
using System.Text.Json;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    internal sealed class MessageParser
    {
        private const int _initialBufferSize = 1024;

        public bool TryParseMessage(
            ReadOnlySequence<byte> slice,
            out OperationMessage? message)
        {
            ReadOnlySpan<byte> messageData;
            byte[] buffer = Array.Empty<byte>();

            if (slice.IsSingleSegment)
            {
                messageData = slice.First.Span;
            }
            else
            {
                buffer = ArrayPool<byte>.Shared.Rent(_initialBufferSize);
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

                    span.CopyTo(buffer.AsSpan().Slice(buffered));
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

                GraphQLSocketMessage parsedMessage = ParseMessage(messageData);
                message = DeserializeMessage(parsedMessage);
                return true;
            }
            //TODO: Catch Syntax exception
            catch (Exception ex)
            {
                message = null;
                return false;
            }
            finally
            {
                if (buffer.Length > 0)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private OperationMessage DeserializeMessage(GraphQLSocketMessage parsedMessage)
        {
            switch (parsedMessage.Type)
            {
                // case MessageTypes.Connection.Error:

                case MessageTypes.Connection.Accept:
                    return AcceptConnectionMessage.Default;

                case MessageTypes.Subscription.Data:
                    return DeserializeSubscriptionResultMessage(parsedMessage);

                // case MessageTypes.Subscription.Error:

                case MessageTypes.Subscription.Complete:
                    return DeserializeSubscriptionCompleteMessage(parsedMessage);

                default:
                    return KeepConnectionAliveMessage.Default;
            }
        }

        private static DataCompleteMessage DeserializeSubscriptionCompleteMessage(
            GraphQLSocketMessage parsedMessage)
        {
            if (parsedMessage.Id is null)
            {
                // TODO : resources
                throw new InvalidOperationException("Invalid message structure.");
            }

            return new DataCompleteMessage(parsedMessage.Id);
        }

        private OperationMessage DeserializeSubscriptionResultMessage(
            GraphQLSocketMessage parsedMessage)
        {
            if (parsedMessage.Id is null)
            {
                // TODO : resources
                throw new InvalidOperationException("Invalid message structure.");
            }

            var reader = new Utf8JsonReader(parsedMessage.Payload);
            return new DataResultMessage(parsedMessage.Id, JsonDocument.ParseValue(ref reader));

            //TODO: Is this needed?
            // return KeepConnectionAliveMessage.Default;
        }

        private static GraphQLSocketMessage ParseMessage(ReadOnlySpan<byte> messageData)
        {
            return new GraphQlMessageParser(messageData).ParseMessage();
        }
    }
}
