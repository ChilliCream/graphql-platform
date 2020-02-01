using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;
using StrawberryShake.Transport;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public sealed class MessageProcessor
    {
        internal const byte Delimiter = 0x07;
        private readonly ISocketConnection _connection;
        private readonly MessageParser _parser;
        private readonly IMessageHandler[] _messageHandlers;
        private readonly PipeReader _reader;

        internal MessageProcessor(
            ISocketConnection connection,
            MessageParser parser,
            IEnumerable<IMessageHandler> messageHandlers,
            PipeReader reader)
        {
            _connection = connection
                ?? throw new ArgumentNullException(nameof(connection));
            _parser = parser
                ?? throw new ArgumentNullException(nameof(parser));
            _messageHandlers = messageHandlers?.ToArray()
                ?? throw new ArgumentNullException(nameof(messageHandlers));
            _reader = reader
                ?? throw new ArgumentNullException(nameof(reader));
        }

        public Task? InnerTask { get; private set; }

        public void Start(CancellationToken cancellationToken)
        {
            if (InnerTask is { })
            {
                return;
            }

            Task.Factory.StartNew(
                () => ProcessMessagesAsync(cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task ProcessMessagesAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    ReadResult result = await _reader
                        .ReadAsync(cancellationToken)
                        .ConfigureAwait(false);

                    ReadOnlySequence<byte> buffer = result.Buffer;
                    SequencePosition? position = null;

                    do
                    {
                        position = buffer.PositionOf(Delimiter);

                        if (position != null)
                        {
                            await ProcessAsync(
                                _connection,
                                buffer.Slice(0, position.Value),
                                cancellationToken)
                                .ConfigureAwait(false);

                            // Skip the message which was read.
                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                        }
                    }
                    while (position != null);

                    _reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                _reader.Complete();
            }
        }

        private Task ProcessAsync(
            ISocketConnection connection,
            ReadOnlySequence<byte> slice,
            CancellationToken cancellationToken)
        {
            if (_parser.TryParseMessage(slice, out OperationMessage? message))
            {
                return HandleMessageAsync(connection, message!, cancellationToken);
            }
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(
            ISocketConnection connection,
            OperationMessage message,
            CancellationToken cancellationToken)
        {
            if (message.Type.Equals(
                MessageTypes.Connection.KeepAlive,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

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

            return;

            // TODO : resources
            // throw new NotSupportedException(
            //    "The specified message type is not supported.");
        }
    }
}
