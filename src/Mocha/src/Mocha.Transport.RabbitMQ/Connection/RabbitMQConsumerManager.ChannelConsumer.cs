using System.Buffers;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

public sealed partial class RabbitMQConsumerManager
{
    internal sealed partial class RegisteredConsumer
    {
        /// <summary>
        /// Queues deliveries from one channel and reports when the channel or consumer is lost.
        /// </summary>
        private sealed class ChannelConsumer : AsyncDefaultBasicConsumer, IDisposable
        {
            private const int NotLost = 0;
            private const int LossPending = 1;
            private const int LossHandled = 2;

            private readonly RegisteredConsumer _owner;
            private readonly CancellationTokenSource _handlerCts = new();
            private readonly CancellationToken _handlerToken;
            private readonly Channel<Delivery> _deliveries;
            private readonly object _handlerSync = new();
            private bool _isAccepting = true;
            private bool _handlersCancelled;
            private int _activeHandlers;
            private int _references = 1;
            private int _isDisposed;
            private int _lossState;
            private ShutdownEventArgs? _lossReason;

            public ChannelConsumer(RegisteredConsumer owner, IChannel channel)
                : base(channel)
            {
                _owner = owner;
                _handlerToken = _handlerCts.Token;
                _deliveries = System.Threading.Channels.Channel.CreateUnbounded<Delivery>();
            }

            public bool TryAcquire()
            {
                var references = Volatile.Read(ref _references);
                while (references != 0)
                {
                    var previous = Interlocked.CompareExchange(ref _references, references + 1, references);
                    if (previous == references)
                    {
                        return true;
                    }

                    references = previous;
                }

                return false;
            }

            public void Release()
            {
                if (Interlocked.Decrement(ref _references) == 0)
                {
                    _handlerCts.Dispose();
                }
            }

            public bool IsLost => Volatile.Read(ref _lossState) != NotLost;

            public ShutdownEventArgs? LossReason => Volatile.Read(ref _lossReason);

            /// <summary>
            /// Records a loss for the owner to handle.
            /// </summary>
            public void MarkLostPending(ShutdownEventArgs? reason)
            {
                Interlocked.CompareExchange(ref _lossReason, reason, null);
                Interlocked.CompareExchange(ref _lossState, LossPending, NotLost);
            }

            /// <summary>
            /// Returns whether a pending loss was recorded, and marks it handled.
            /// </summary>
            public bool TryHandlePendingLoss()
                => Interlocked.CompareExchange(ref _lossState, LossHandled, LossPending) == LossPending;

            public void CancelHandlers()
            {
                lock (_handlerSync)
                {
                    if (_handlersCancelled)
                    {
                        return;
                    }

                    _handlersCancelled = true;
                    Interlocked.Add(ref _owner._cancelledHandlers, _activeHandlers);
                }

                _owner.UpdateRecoveryState();
                if (TryAcquire())
                {
                    _ = CancelHandlersAsync();
                }
            }

            private async Task CancelHandlersAsync()
            {
                try
                {
                    await _handlerCts.CancelAsync();
                }
                catch (Exception ex)
                {
                    _owner.Manager.Logger.ErrorCancellingConsumerHandlers(ex, _owner.Manager.ConnectionName, _owner.QueueName);
                }
                finally
                {
                    Release();
                }
            }

            /// <summary>
            /// Rejects further deliveries and discards deliveries whose handlers have not started.
            /// </summary>
            public void StopAccepting()
            {
                lock (_handlerSync)
                {
                    _isAccepting = false;
                }

                _deliveries.Writer.TryComplete();
                while (_deliveries.Reader.TryRead(out var delivery))
                {
                    ArrayPool<byte>.Shared.Return(delivery.Buffer);
                }
            }

            public override Task HandleBasicDeliverAsync(
                string consumerTag,
                ulong deliveryTag,
                bool redelivered,
                string exchange,
                string routingKey,
                IReadOnlyBasicProperties properties,
                ReadOnlyMemory<byte> body,
                CancellationToken cancellationToken = default)
            {
                if (!Volatile.Read(ref _isAccepting))
                {
                    // The channel is lost or stopping, so the broker requeues the delivery when the channel closes.
                    return Task.CompletedTask;
                }

                // The client returns the body's buffer to its pool once this callback returns.
                var buffer = ArrayPool<byte>.Shared.Rent(body.Length);
                body.CopyTo(buffer);

                var eventArgs = new BasicDeliverEventArgs(
                    consumerTag,
                    deliveryTag,
                    redelivered,
                    exchange,
                    routingKey,
                    properties,
                    buffer.AsMemory(0, body.Length),
                    _handlerToken);

                if (!_deliveries.Writer.TryWrite(new Delivery(eventArgs, buffer)))
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                return Task.CompletedTask;
            }

            public override async Task HandleBasicCancelAsync(
                string consumerTag,
                CancellationToken cancellationToken = default)
            {
                await base.HandleBasicCancelAsync(consumerTag, cancellationToken);
                _owner.OnConsumerLost(this, reason: null);
                StopAccepting();
                CancelHandlers();
            }

            public Task OnChannelShutdownAsync(object? _, ShutdownEventArgs reason)
            {
                _owner.OnConsumerLost(this, reason);
                StopAccepting();
                CancelHandlers();
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
                {
                    StopAccepting();
                    Release();
                }
            }

            public async Task ProcessDeliveriesAsync(CancellationToken cancellationToken)
            {
                var reader = _deliveries.Reader;
                while (await reader.WaitToReadAsync(cancellationToken))
                {
                    while (reader.TryRead(out var delivery))
                    {
                        try
                        {
                            if (TryStartHandler())
                            {
                                try
                                {
                                    await _owner.MessageHandler(Channel, delivery.EventArgs, _handlerToken);
                                }
                                finally
                                {
                                    FinishHandler();
                                }
                            }
                        }
                        catch (OperationCanceledException) when (_handlerToken.IsCancellationRequested)
                        {
                            // The channel was lost or the shutdown deadline elapsed.
                        }
                        catch (Exception ex)
                        {
                            _owner.Manager.Logger.ErrorProcessingMessageFromQueue(ex, _owner.Manager.ConnectionName, _owner.QueueName);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(delivery.Buffer);
                        }
                    }
                }
            }

            private bool TryStartHandler()
            {
                lock (_handlerSync)
                {
                    if (!_isAccepting || IsLost || _handlersCancelled || _owner._consumerToken.IsCancellationRequested)
                    {
                        return false;
                    }

                    _activeHandlers++;
                    return true;
                }
            }

            private void FinishHandler()
            {
                bool cancelled;
                lock (_handlerSync)
                {
                    _activeHandlers--;
                    cancelled = _handlersCancelled;
                    if (cancelled)
                    {
                        Interlocked.Decrement(ref _owner._cancelledHandlers);
                    }
                }

                if (cancelled)
                {
                    _owner.UpdateRecoveryState();
                }
            }

            private readonly record struct Delivery(BasicDeliverEventArgs EventArgs, byte[] Buffer);
        }
    }
}
