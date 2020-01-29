using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace MarshmallowPie.Processing.AzureServiceBus
{
    public class MessageReceiver<TMessage>
        : IMessageReceiver<TMessage>
        where TMessage : class
    {
        private readonly Func<ISubscriptionClient> _subscriptionClientFactory;

        public MessageReceiver(Func<ISubscriptionClient> subscriptionClientFactory)
        {
            _subscriptionClientFactory = subscriptionClientFactory;
        }

        public ValueTask<IMessageStream<TMessage>> SubscribeAsync(
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<IMessageStream<TMessage>>(
                new MessageStream(_subscriptionClientFactory()));
        }

        private class MessageStream
            : IMessageStream<TMessage>
        {
            private readonly ISubscriptionClient _subscriptionClient;
            private MessageEnumerator? _messageEnumerator;

            public MessageStream(ISubscriptionClient subscriptionClient)
            {
                _subscriptionClient = subscriptionClient;
            }

            public IAsyncEnumerator<TMessage?> GetAsyncEnumerator(
                CancellationToken cancellationToken = default)
            {
                if (_messageEnumerator is null)
                {
                    _messageEnumerator = new MessageEnumerator(
                        _subscriptionClient, cancellationToken);
                }

                return _messageEnumerator;
            }

            public ValueTask CompleteAsync(CancellationToken cancellationToken = default)
            {
                if (_messageEnumerator is null)
                {
                    throw new InvalidOperationException("There is now message selected.");
                }

                return _messageEnumerator.CompleteAsync(cancellationToken);
            }
        }

        private class MessageEnumerator
            : IAsyncEnumerator<TMessage?>
            , IDisposable
        {
            private readonly ISubscriptionClient _subscriptionClient;
            private readonly SemaphoreSlim _completeSemaphore = new SemaphoreSlim(0, 1);
            private readonly CancellationToken _cancellationToken;
            private TaskCompletionSource<TMessage> _messageResult =
                new TaskCompletionSource<TMessage>();
            private Message? _message;

            public MessageEnumerator(
                ISubscriptionClient subscriptionClient,
                CancellationToken cancellationToken)
            {
                _subscriptionClient = subscriptionClient;
                _cancellationToken = cancellationToken;

                subscriptionClient.RegisterMessageHandler(
                    ReceiveMessageAsync,
                    new MessageHandlerOptions(ex => Task.CompletedTask)
                    {
                        MaxConcurrentCalls = 1,
                        AutoComplete = false
                    });

                cancellationToken.Register(() => _messageResult.TrySetCanceled());
            }

            public TMessage? Current { get; private set; }

            private async Task ReceiveMessageAsync(Message message, CancellationToken cancellationToken)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                _message = message;

                TMessage body = JsonSerializer.Deserialize<TMessage>(message.Body);
                _messageResult.SetResult(body);

                await _completeSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                Current = await _messageResult.Task.ConfigureAwait(false);
                _messageResult = new TaskCompletionSource<TMessage>();
                return true;
            }

            public async ValueTask CompleteAsync(CancellationToken cancellationToken)
            {
                if (_message is null)
                {
                    throw new InvalidOperationException("There is now message selected.");
                }

                await _subscriptionClient.CompleteAsync(
                    _message.SystemProperties.LockToken)
                    .ConfigureAwait(false);
                _message = null;
                _completeSemaphore.Release();
            }

            public async ValueTask DisposeAsync()
            {
                await _subscriptionClient.CloseAsync().ConfigureAwait(false);
                _messageResult.TrySetCanceled();
                Dispose();
            }

            public void Dispose()
            {
                _completeSemaphore.Dispose();
            }
        }
    }
}
