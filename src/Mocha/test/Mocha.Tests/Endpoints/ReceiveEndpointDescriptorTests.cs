using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha.Tests;

/// <summary>
/// Unit tests for the base <see cref="ReceiveEndpointDescriptor{T}"/> AutoBind, BindFrom,
/// and Receives(configure) surface.
/// </summary>
public class ReceiveEndpointDescriptorTests
{
    // Minimal concrete subclass used to exercise the base descriptor methods without
    // standing up a full transport runtime.
    private sealed class TestDescriptor(IMessagingConfigurationContext context)
        : ReceiveEndpointDescriptor<ReceiveEndpointConfiguration>(context)
    {
        public TestDescriptor() : this(new StubContext())
        {
            Configuration = new ReceiveEndpointConfiguration { Name = "test" };
        }

        public new ReceiveEndpointConfiguration Configuration
        {
            get => base.Configuration;
            set => base.Configuration = value;
        }
    }

    private sealed class StubContext : IMessagingConfigurationContext
    {
        public IServiceProvider Services => throw new NotSupportedException();
        public IBusNamingConventions Naming => throw new NotSupportedException();
        public IMessageTypeRegistry Messages => throw new NotSupportedException();
        public IMessageRouter Router => throw new NotSupportedException();
        public IEndpointRouter Endpoints => throw new NotSupportedException();
        public IHostInfo Host => throw new NotSupportedException();
        public IConventionRegistry Conventions => throw new NotSupportedException();
        public ImmutableHashSet<Consumer> Consumers => throw new NotSupportedException();
        public ImmutableArray<MessagingTransport> Transports => throw new NotSupportedException();
        public IFeatureCollection Features => throw new NotSupportedException();
    }

    [Fact]
    public void Receives_Should_RecordTypeBindIntent_When_ConfigureCallbackUsed()
    {
        // arrange
        var descriptor = new TestDescriptor();
        var source = new Uri("queue:my-source");

        // act
        descriptor.Receives<OrderCreated>(r => r.BindFrom(source, "my-key"));

        // assert: the type is registered and the intent captures the BindFrom and implied AutoBind(false)
        Assert.True(descriptor.Configuration.TypeBinds.TryGetValue(typeof(OrderCreated), out var intent));
        Assert.Equal(typeof(OrderCreated), intent.MessageType);
        Assert.Equal(false, intent.AutoBind);
        var bindFrom = Assert.Single(intent.BindFroms);
        Assert.Equal(source, bindFrom.Source);
        Assert.Equal("my-key", bindFrom.RoutingKey);
    }

    [Fact]
    public void BindFrom_Should_AppendToQueueBindFroms_When_CalledAtQueueScope()
    {
        // arrange
        var descriptor = new TestDescriptor();
        var source1 = new Uri("queue:source-a");
        var source2 = new Uri("queue:source-b");

        // act
        descriptor.BindFrom(source1);
        descriptor.BindFrom(source2, "rk");

        // assert: both intents are accumulated in order; TypeBinds is unaffected
        Assert.Equal(2, descriptor.Configuration.QueueBindFroms.Count);
        Assert.Equal(source1, descriptor.Configuration.QueueBindFroms[0].Source);
        Assert.Null(descriptor.Configuration.QueueBindFroms[0].RoutingKey);
        Assert.Equal(source2, descriptor.Configuration.QueueBindFroms[1].Source);
        Assert.Equal("rk", descriptor.Configuration.QueueBindFroms[1].RoutingKey);
        Assert.Empty(descriptor.Configuration.TypeBinds);
    }

    [Fact]
    public void Receives_Should_NotAffectQueueAutoBindSetting_When_TypeLevelBindFromUsed()
    {
        // arrange
        var descriptor = new TestDescriptor();

        // act: per-type BindFrom implies AutoBind(false) for that type only, not the queue
        descriptor.Receives<OrderCreated>(r => r.BindFrom(new Uri("queue:src")));

        // assert: queue-level AutoBind is unset; type-level intent carries false
        Assert.Null(descriptor.Configuration.AutoBind);
        Assert.Equal(false, descriptor.Configuration.TypeBinds[typeof(OrderCreated)].AutoBind);
    }

    [Fact]
    public void Receives_Should_MergeIntoSingleTypeBindEntry_When_CalledTwiceForSameType()
    {
        // arrange
        var descriptor = new TestDescriptor();
        var source1 = new Uri("queue:source-a");
        var source2 = new Uri("queue:source-b");

        // act: two separate Receives<T> calls with different BindFrom sources
        descriptor.Receives<OrderCreated>(r => r.BindFrom(source1));
        descriptor.Receives<OrderCreated>(r => r.BindFrom(source2, "k2"));

        // assert: one entry in TypeBinds, both BindFroms accumulated; ReceivedMessageTypes may
        // contain duplicates (the unique-entry guarantee lives in TypeBinds)
        Assert.Single(descriptor.Configuration.TypeBinds);
        var intent = descriptor.Configuration.TypeBinds[typeof(OrderCreated)];
        Assert.Equal(2, intent.BindFroms.Count);
        Assert.Equal(source1, intent.BindFroms[0].Source);
        Assert.Equal(source2, intent.BindFroms[1].Source);
    }
}
