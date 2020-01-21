using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public sealed class Subscription<T>
        : Subscription
        , IResponseStream<T> where T : class
    {
        private Channel<IOperationResult<T>> _channel;
        private Func<Task>? _unregister;
        private bool _disposed;

        public Subscription(
            IOperation operation,
            IOperationFormatter operationFormatter,
            IResultParser resultParser)
        {
            Id = Guid.NewGuid().ToString("N");
            Operation = operation
                ?? throw new ArgumentNullException(nameof(operation));
            OperationFormatter = operationFormatter
                ?? throw new ArgumentNullException(nameof(operationFormatter));
            ResultParser = resultParser
                ?? throw new ArgumentNullException(nameof(resultParser));
            _channel = Channel.CreateBounded<IOperationResult<T>>(4);
        }

        public override string Id { get; }

        public override IOperation Operation { get; }

        public override IOperationFormatter OperationFormatter { get; }

        public override IResultParser ResultParser { get; }

        public async IAsyncEnumerator<IOperationResult<T>> GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                yield break;
            }

            ChannelReader<IOperationResult<T>> reader = _channel.Reader;

            while (!_disposed && !reader.Completion.IsCompleted)
            {
                if (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        protected override IAsyncEnumerator<IOperationResult> OnGetAsyncEnumerator(
            CancellationToken cancellationToken) =>
            GetAsyncEnumerator(cancellationToken);

        public override void OnRegister(Func<Task> unregister)
        {
            _unregister = unregister ?? throw new ArgumentNullException(nameof(unregister));
        }

        public override ValueTask OnReceiveResultAsync(
            DataResultMessage message,
            CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return OnReceiveResultInternal(message, cancellationToken);
        }

        private async ValueTask OnReceiveResultInternal(
            DataResultMessage message,
            CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return;
            }

            if (await _channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                await InvokeSubscriptionMiddleware(message.Payload).ConfigureAwait(false);
                var result = (OperationResult<T>)message.Payload.Build();
                await _channel.Writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
            }
        }

        private ValueTask InvokeSubscriptionMiddleware(IOperationResultBuilder builder)
        {
            return default;
        }

        public override ValueTask OnCompletedAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return default;
            }

            _channel.Writer.Complete();
            return default;
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (!_channel.Reader.Completion.IsCompleted)
                {
                    _channel.Writer.Complete();
                }

                if (_unregister is { })
                {
                    await _unregister().ConfigureAwait(false);
                    _unregister = null;
                }

                _disposed = true;
            }
        }
    }
}
