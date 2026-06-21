using System.Buffers.Binary;
using System.Globalization;
using System.IO.Hashing;
using System.Text;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages the RabbitMQ topology model (exchanges, queues, and bindings) for a transport instance,
/// providing thread-safe mutation and lookup of topology resources.
/// </summary>
public sealed class RabbitMQMessagingTopology(
    RabbitMQMessagingTransport transport,
    Uri baseAddress,
    RabbitMQBusDefaults defaults,
    bool autoProvision)
    : MessagingTopology<RabbitMQMessagingTransport>(transport, baseAddress)
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly List<RabbitMQExchange> _exchanges = [];
    private readonly List<RabbitMQQueue> _queues = [];
    private readonly List<RabbitMQBinding> _bindings = [];
    private readonly Dictionary<BindingKey, RabbitMQBinding> _bindingsByKey = [];

    /// <summary>
    /// Gets a value indicating whether topology resources should be auto-provisioned by default.
    /// Individual resources may override this setting via their own <c>AutoProvision</c> property.
    /// </summary>
    public bool AutoProvision => autoProvision;

    /// <summary>
    /// Gets the list of exchanges registered in this topology.
    /// </summary>
    public IReadOnlyList<RabbitMQExchange> Exchanges => _exchanges;

    /// <summary>
    /// Gets the list of queues registered in this topology.
    /// </summary>
    public IReadOnlyList<RabbitMQQueue> Queues => _queues;

    /// <summary>
    /// Gets the list of bindings connecting exchanges to queues or other exchanges in this topology.
    /// </summary>
    public IReadOnlyList<RabbitMQBinding> Bindings => _bindings;

    /// <summary>
    /// Gets the bus-level defaults applied to all auto-provisioned queues and exchanges.
    /// </summary>
    public RabbitMQBusDefaults Defaults => defaults;

    /// <summary>
    /// Gets the exchange with the same name, or creates it from the provided configuration.
    /// Existing exchanges are returned unchanged.
    /// </summary>
    public RabbitMQExchange GetOrAddExchange(
        string name,
        Func<string, RabbitMQExchangeConfiguration> factory)
    {
        lock (_lock)
        {
            var exchange = _exchanges.FirstOrDefault(e => e.Name == name);
            if (exchange is not null)
            {
                return exchange;
            }

            var configuration = factory(name);
            configuration.Name = name;
            return CreateExchange(configuration);
        }
    }

    /// <summary>
    /// Adds an exchange to the topology, or returns the existing exchange with the same name.
    /// </summary>
    public RabbitMQExchange AddExchange(RabbitMQExchangeConfiguration configuration)
    {
        lock (_lock)
        {
            var exchange = _exchanges.FirstOrDefault(e => e.Name == configuration.Name);
            if (exchange is not null)
            {
                return exchange;
            }

            return CreateExchange(configuration);
        }
    }

    private RabbitMQExchange CreateExchange(RabbitMQExchangeConfiguration configuration)
    {
        var exchange = new RabbitMQExchange();

        configuration.Topology = this;
        defaults.Exchange.ApplyTo(configuration);
        exchange.Initialize(configuration);

        _exchanges.Add(exchange);

        exchange.Complete();

        return exchange;
    }

    /// <summary>
    /// Gets the queue with the same name, or creates it from the provided configuration.
    /// Existing queues are returned unchanged.
    /// </summary>
    public RabbitMQQueue GetOrAddQueue(
        string name,
        Func<string, RabbitMQQueueConfiguration> factory)
    {
        lock (_lock)
        {
            var queue = _queues.FirstOrDefault(q => q.Name == name);
            if (queue is not null)
            {
                return queue;
            }

            var configuration = factory(name);
            configuration.Name = name;
            return CreateQueue(configuration);
        }
    }

    /// <summary>
    /// Adds a queue to the topology, or returns the existing queue with the same name.
    /// </summary>
    public RabbitMQQueue AddQueue(RabbitMQQueueConfiguration configuration)
    {
        lock (_lock)
        {
            var queue = _queues.FirstOrDefault(q => q.Name == configuration.Name);
            if (queue is not null)
            {
                return queue;
            }

            return CreateQueue(configuration);
        }
    }

    private RabbitMQQueue CreateQueue(RabbitMQQueueConfiguration configuration)
    {
        configuration.Topology = this;

        defaults.Queue.ApplyTo(configuration);

        var queue = new RabbitMQQueue();
        queue.Initialize(configuration);

        _queues.Add(queue);

        queue.Complete();

        return queue;
    }

    /// <summary>
    /// Ensures that a binding with the same identity exists in the topology.
    /// </summary>
    public void EnsureBinding(
        string source,
        string destination,
        RabbitMQDestinationKind destinationKind,
        Func<string, string, RabbitMQDestinationKind, RabbitMQBindingConfiguration> factory)
    {
        lock (_lock)
        {
            var configuration = factory(source, destination, destinationKind);
            configuration.Source = source;
            configuration.Destination = destination;
            configuration.DestinationKind = destinationKind;

            var key = BindingKey.Create(configuration);
            if (_bindingsByKey.TryGetValue(key, out _))
            {
                return;
            }

            CreateBinding(configuration);
        }
    }

    /// <summary>
    /// Adds a binding to the topology if one with the same identity does not already exist.
    /// </summary>
    public void AddBinding(RabbitMQBindingConfiguration configuration)
    {
        lock (_lock)
        {
            var key = BindingKey.Create(configuration);
            if (_bindingsByKey.TryGetValue(key, out _))
            {
                return;
            }

            CreateBinding(configuration);
        }
    }

    private void CreateBinding(RabbitMQBindingConfiguration configuration)
    {
        var source = _exchanges.FirstOrDefault(e => e.Name == configuration.Source);
        if (source is null)
        {
            throw new InvalidOperationException($"Source exchange '{configuration.Source}' not found");
        }

        RabbitMQBinding binding;

        if (configuration.DestinationKind == RabbitMQDestinationKind.Queue)
        {
            var destination = _queues.FirstOrDefault(q => q.Name == configuration.Destination);
            if (destination is null)
            {
                throw new InvalidOperationException($"Destination queue '{configuration.Destination}' not found");
            }

            var queueBinding = new RabbitMQQueueBinding();
            configuration.Topology = this;
            queueBinding.Initialize(configuration);
            queueBinding.SetDestination(destination);
            destination.AddBinding(queueBinding);

            binding = queueBinding;
        }
        else if (configuration.DestinationKind == RabbitMQDestinationKind.Exchange)
        {
            var destination = _exchanges.FirstOrDefault(e => e.Name == configuration.Destination);
            if (destination is null)
            {
                throw new InvalidOperationException(
                    $"Destination exchange '{configuration.Destination}' not found");
            }

            var exchangeBinding = new RabbitMQExchangeBinding();
            configuration.Topology = this;
            exchangeBinding.Initialize(configuration);
            exchangeBinding.SetDestination(destination);
            destination.AddBinding(exchangeBinding);

            binding = exchangeBinding;
        }
        else
        {
            throw new InvalidOperationException($"Unknown destination kind: {configuration.DestinationKind}");
        }

        binding.SetSource(source);
        source.AddBinding(binding);

        _bindings.Add(binding);

        _bindingsByKey.Add(BindingKey.Create(configuration), binding);

        binding.Complete();
    }

    private readonly record struct BindingKey(
        string Source,
        string Destination,
        RabbitMQDestinationKind DestinationKind,
        string RoutingKey,
        int ArgumentCount,
        ulong ArgumentsHash)
    {
        public static BindingKey Create(RabbitMQBindingConfiguration configuration)
        {
            var arguments = configuration.Arguments;

            return new(
                configuration.Source,
                configuration.Destination,
                configuration.DestinationKind,
                configuration.RoutingKey ?? string.Empty,
                arguments?.Count ?? 0,
                HashArguments(arguments));
        }

        private static ulong HashArguments(IDictionary<string, object>? arguments)
        {
            if (arguments is not { Count: > 0 })
            {
                return 0;
            }

            var hash = new XxHash64();
            AppendInt32(ref hash, arguments.Count);

            foreach (var (key, value) in arguments.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                AppendString(ref hash, key);
                AppendValue(ref hash, value);
            }

            return hash.GetCurrentHashAsUInt64();
        }

        private static void AppendValue(ref XxHash64 hash, object? value)
        {
            if (value is null)
            {
                AppendByte(ref hash, 0);
                return;
            }

            if (value is byte[] bytes)
            {
                AppendByte(ref hash, 1);
                AppendBytes(ref hash, bytes);
                return;
            }

            AppendByte(ref hash, 2);
            AppendString(ref hash, value.GetType().FullName ?? value.GetType().Name);
            AppendString(ref hash, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        private static void AppendString(ref XxHash64 hash, string value)
        {
            var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);

            if (maxByteCount <= 256)
            {
                Span<byte> buffer = stackalloc byte[maxByteCount];
                var bytesWritten = Encoding.UTF8.GetBytes(value, buffer);
                AppendBytes(ref hash, buffer[..bytesWritten]);
            }
            else
            {
                AppendBytes(ref hash, Encoding.UTF8.GetBytes(value));
            }
        }

        private static void AppendBytes(ref XxHash64 hash, ReadOnlySpan<byte> bytes)
        {
            AppendInt32(ref hash, bytes.Length);
            hash.Append(bytes);
        }

        private static void AppendInt32(ref XxHash64 hash, int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            hash.Append(buffer);
        }

        private static void AppendByte(ref XxHash64 hash, byte value)
        {
            Span<byte> buffer = [value];
            hash.Append(buffer);
        }
    }
}
