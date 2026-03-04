using System.Buffers;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Minimal IMessagingSetupContext for unit testing saga initialization and descriptor tests.
/// </summary>
public sealed class TestMessagingSetupContext : IMessagingSetupContext
{
    private static readonly Lazy<TestMessagingSetupContext> _instance = new(() => new());

    public static TestMessagingSetupContext Instance => _instance.Value;

    public IServiceProvider Services { get; }
    public IBusNamingConventions Naming { get; } = new TestNamingConventions();
    public IFeatureCollection Features { get; } = new FeatureCollection();
    public IMessageTypeRegistry Messages => throw new NotSupportedException("Not available in test context");
    public IMessageRouter Router => throw new NotSupportedException("Not available in test context");
    public IEndpointRouter Endpoints => throw new NotSupportedException("Not available in test context");
    public IHostInfo Host => throw new NotSupportedException("Not available in test context");
    public IConventionRegistry Conventions => throw new NotSupportedException("Not available in test context");
    public ImmutableHashSet<Consumer> Consumers => ImmutableHashSet<Consumer>.Empty;
    public ImmutableArray<MessagingTransport> Transports => [];
    public MessagingTransport? Transport => null;

    public TestMessagingSetupContext()
    {
        Services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ISagaStateSerializerFactory>(new TestSagaStateSerializerFactory())
            .BuildServiceProvider();
    }

    private sealed class TestNamingConventions : IBusNamingConventions
    {
        public string GetSagaName(Type sagaType)
        {
            var name = sagaType.Name;

            // Strip generic arity suffix (e.g. "FluentSaga`1" → "FluentSaga")
            var idx = name.IndexOf('`');
            if (idx > 0)
            {
                name = name[..idx];
            }

            // Append generic args for readability
            if (sagaType.IsGenericType)
            {
                var args = string.Join(", ", sagaType.GetGenericArguments().Select(t => t.Name));
                name = $"{name}<{args}>";
            }

            // Prefix with declaring type if nested
            if (sagaType.DeclaringType is { } declaring)
            {
                name = $"{declaring.Name}.{name}";
            }

            return name;
        }

        public string GetReceiveEndpointName(InboundRoute route, ReceiveEndpointKind kind)
            => throw new NotSupportedException();

        public string GetReceiveEndpointName(Type handlerType, ReceiveEndpointKind kind)
            => throw new NotSupportedException();

        public string GetReceiveEndpointName(string name, ReceiveEndpointKind kind)
            => throw new NotSupportedException();

        public string GetInstanceEndpoint(Guid instanceId) => throw new NotSupportedException();

        public string GetSendEndpointName(Type messageType) => throw new NotSupportedException();

        public string GetPublishEndpointName(Type messageType) => throw new NotSupportedException();

        public string GetMessageIdentity(Type messageType) => throw new NotSupportedException();
    }

    private sealed class TestSagaStateSerializerFactory : ISagaStateSerializerFactory
    {
        public ISagaStateSerializer GetSerializer(Type type) => new TestSagaStateSerializer();
    }

    private sealed class TestSagaStateSerializer : ISagaStateSerializer
    {
        public T? Deserialize<T>(ReadOnlyMemory<byte> body) => default;

        public object? Deserialize(ReadOnlyMemory<byte> body) => null;

        public void Serialize<T>(T message, IBufferWriter<byte> writer) { }

        public void Serialize(object message, IBufferWriter<byte> writer) { }
    }
}
