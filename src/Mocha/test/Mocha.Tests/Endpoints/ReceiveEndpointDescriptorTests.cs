using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha.Tests;

/// <summary>
/// Unit tests for the base <see cref="ReceiveEndpointDescriptor{T}"/> AutoBind and Receives surface.
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

        // act
        descriptor.Receives<OrderCreated>(r => r.AutoBind(false));

        // assert
        Assert.True(descriptor.Configuration.TypeBinds.TryGetValue(typeof(OrderCreated), out var intent));
        Assert.Equal(typeof(OrderCreated), intent.MessageType);
        Assert.Equal(false, intent.AutoBind);
    }

    [Fact]
    public void Receives_Should_NotAffectQueueAutoBindSetting_When_TypeLevelAutoBindUsed()
    {
        // arrange
        var descriptor = new TestDescriptor();

        // act: per-type AutoBind(false) affects only that type, not the queue
        descriptor.Receives<OrderCreated>(r => r.AutoBind(false));

        // assert: queue-level AutoBind is unset; type-level intent carries false
        Assert.Null(descriptor.Configuration.AutoBind);
        Assert.Equal(false, descriptor.Configuration.TypeBinds[typeof(OrderCreated)].AutoBind);
    }

    [Fact]
    public void Receives_Should_MergeIntoSingleTypeBindEntry_When_CalledTwiceForSameType()
    {
        // arrange
        var descriptor = new TestDescriptor();

        // act: two separate Receives<T> calls; the second one sets AutoBind(false)
        descriptor.Receives<OrderCreated>(r => r.AutoBind(true));
        descriptor.Receives<OrderCreated>(r => r.AutoBind(false));

        // assert: one entry in TypeBinds; last explicit wins via merge
        Assert.Single(descriptor.Configuration.TypeBinds);
        var intent = descriptor.Configuration.TypeBinds[typeof(OrderCreated)];
        Assert.Equal(false, intent.AutoBind);
    }
}
