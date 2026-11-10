using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

public sealed partial class RabbitMQDispatcher
{
    private sealed class PublisherChannelPool
    {
        private readonly CancellationTokenSource _retirementCts = new();
        private readonly Dictionary<object, PublisherChannelSlot> _publishers = new(ReferenceEqualityComparer.Instance);
        private readonly LinkedList<PublisherChannelSlot> _available = new();
        private readonly int _capacity;
        private TaskCompletionSource? _slotAvailable;

        public PublisherChannelPool(IConnection connection)
        {
            Connection = connection;
            RetirementToken = _retirementCts.Token;
            _capacity = connection.ChannelMax == 0
                ? MaxPublisherChannels
                : Math.Min(MaxPublisherChannels, (int)connection.ChannelMax);
            Slots = new List<PublisherChannelSlot>(_capacity);
        }

        public IConnection Connection { get; }

        public List<PublisherChannelSlot> Slots { get; }

        public CancellationToken RetirementToken { get; }

        public PublisherChannelSlot? TryRentSlot(object? publisher)
        {
            PublisherChannelSlot slot;
            if (publisher is not null && _publishers.TryGetValue(publisher, out var shared))
            {
                slot = shared;
                if (slot.ActiveRentals == 0)
                {
                    _available.Remove(slot.Node);
                }
            }
            else
            {
                if (_available.Last is { } available)
                {
                    slot = available.Value;
                    _available.RemoveLast();
                    if (slot.Publisher is { } previousPublisher)
                    {
                        _publishers.Remove(previousPublisher);
                    }
                }
                else if (Slots.Count < _capacity)
                {
                    slot = new PublisherChannelSlot(this);
                    Slots.Add(slot);
                }
                else
                {
                    return null;
                }

                slot.Publisher = publisher;
                if (publisher is not null)
                {
                    _publishers.Add(publisher, slot);
                    SignalSlotAvailable();
                }
            }

            slot.ActiveRentals++;
            return slot;
        }

        public void ReturnSlot(PublisherChannelSlot slot)
        {
            if (--slot.ActiveRentals == 0)
            {
                _available.AddLast(slot.Node);
                SignalSlotAvailable();
            }
        }

        public Task WaitForSlot()
            => (_slotAvailable ??= new(TaskCreationOptions.RunContinuationsAsynchronously)).Task;

        private void SignalSlotAvailable()
        {
            var available = _slotAvailable;
            _slotAvailable = null;
            available?.TrySetResult();
        }

        public async Task RetireAsync()
        {
            await _retirementCts.CancelAsync();
            _retirementCts.Dispose();
        }
    }

    private sealed class PublisherChannelSlot
    {
        public PublisherChannelSlot(PublisherChannelPool pool)
        {
            Pool = pool;
            Node = new LinkedListNode<PublisherChannelSlot>(this);
        }

        public PublisherChannelPool Pool { get; }

        public LinkedListNode<PublisherChannelSlot> Node { get; }

        public object? Publisher { get; set; }

        public int ActiveRentals { get; set; }

        public PublisherChannel? Current { get; set; }

        public Task Cleanup { get; set; } = Task.CompletedTask;
    }

    private sealed class PublisherChannel(PublisherChannelSlot slot)
    {
        private readonly TaskCompletionSource<IChannel> _opened =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PublisherChannelSlot Slot { get; } = slot;

        public IConnection Connection => Slot.Pool.Connection;

        public int RentCount { get; set; }

        /// <summary>
        /// Gets the task that completes with the channel once it is open, or fails when it cannot be opened.
        /// </summary>
        public Task<IChannel> Opened => _opened.Task;

        /// <summary>
        /// Gets or sets the task that opens the channel and cleans up after a failed attempt.
        /// </summary>
        public Task Creation { get; set; } = Task.CompletedTask;

        /// <summary>
        /// Gets whether the channel could not be opened or has closed since.
        /// </summary>
        public bool IsUnusable => Opened.IsFaulted || (Opened.IsCompletedSuccessfully && !Opened.Result.IsOpen);

        public void SetOpened(IChannel channel) => _opened.TrySetResult(channel);

        public void SetFailed(Exception exception)
        {
            _opened.TrySetException(exception);

            // Reading the exception marks it as observed when no renter awaits the channel.
            _ = _opened.Task.Exception;
        }
    }
}
