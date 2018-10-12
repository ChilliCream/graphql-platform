using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Subscriptions
{
    public interface ISubscriptionManager
    {
        object Subscribe(QueryRequest queryRequest, string eventName, object args);
        void Unsubscribe(string subscriptionId);
        object Restore(string subscriptionId);
    }


    public interface IEventRegistry
    {
        Task<IEventStream> SubscribeAsync(Event @event);
    }

    public class InMemoryEventRegistry
        : IEventRegistry
        , IEventSender
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Dictionary<Event, List<InMemoryEventStream>> _streams =
            new Dictionary<Event, List<InMemoryEventStream>>();

        public async Task SendAsync(Event @event)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (_streams.TryGetValue(@event, out var subscribers))
                {
                    foreach (var stream in subscribers)
                    {
                        stream.Trigger();
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEventStream> SubscribeAsync(Event @event)
        {
            await _semaphore.WaitAsync();

            try
            {
                if (!_streams.TryGetValue(@event, out var subscribers))
                {
                    subscribers = new List<InMemoryEventStream>();
                    _streams[@event] = subscribers;
                }

                var stream = new InMemoryEventStream();
                stream.Disposed += (s, e) => Unsubscribe(@event, stream);
                subscribers.Add(stream);
                return stream;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void Unsubscribe(Event @event, InMemoryEventStream stream)
        {
            _semaphore.Wait();

            try
            {
                if (_streams.TryGetValue(@event, out var subscribers))
                {
                    subscribers.Remove(stream);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public class InMemoryEventStream
        : IEventStream
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private TaskCompletionSource<object> _taskCompletionSource =
            new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public event EventHandler Disposed;

        public string SubscriptionId { get; } = Guid.NewGuid().ToString("N");

        public bool IsCompleted { get; private set; }

        public void Trigger()
        {
            _semaphore.Wait();

            try
            {
                _taskCompletionSource.TrySetResult(null);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task NextAsync(
            CancellationToken cancellationToken = default)
        {
            await _taskCompletionSource.Task;
            await _semaphore.WaitAsync();

            try
            {
                _taskCompletionSource = new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            IsCompleted = true;
            Disposed?.Invoke(this, EventArgs.Empty);
        }
    }

    public interface IEventStream
        : IDisposable
    {
        string SubscriptionId { get; }

        Task NextAsync(CancellationToken cancellationToken = default);

        bool IsCompleted { get; }
    }

    public interface IEventSender
    {
        Task SendAsync(Event @event);
    }

    public sealed class Event
        : IEquatable<Event>
    {
        public Event(string name)
            : this(name, Array.Empty<ArgumentNode>())
        {
        }

        public Event(string name, IEnumerable<ArgumentNode> arguments)
            : this(name, arguments.ToArray())
        {
        }

        public Event(string name, params ArgumentNode[] arguments)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The event name cannot be null or empty.",
                    nameof(name));
            }

            Name = name;
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
        }

        public string Name { get; }

        public IReadOnlyCollection<ArgumentNode> Arguments { get; }

        public bool Equals(Event other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Name.Equals(other.Name, StringComparison.Ordinal)
                && Arguments.Count == other.Arguments.Count)
            {
                Dictionary<string, IValueNode> arguments =
                    other.Arguments.ToDictionary(
                        c => c.Name.Value,
                        c => c.Value);

                foreach (ArgumentNode argument in Arguments)
                {
                    if (!arguments.TryGetValue(argument.Name.Value, out var v)
                        || !v.Equals(argument.Value))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override bool Equals(object other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other as Event);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Name.GetHashCode() * 379;
                foreach (ArgumentNode argument in Arguments)
                {
                    hash ^= (argument.Name.GetHashCode() * 7);
                    hash ^= (argument.Value.GetHashCode() * 11);
                }
                return hash;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

