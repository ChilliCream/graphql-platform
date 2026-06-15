using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha.Tests;

/// <summary>
/// Unit tests for the base <see cref="ReceiveEndpointDescriptor{T}"/> bind mode and Receives surface.
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
        descriptor.Receives<OrderCreated>(r => r.BindExplicitly());

        // assert
        Assert.True(descriptor.Configuration.TypeBinds.TryGetValue(typeof(OrderCreated), out var intent));
        Assert.Equal(typeof(OrderCreated), intent.MessageType);
        Assert.Equal(MessagingBindMode.Explicit, intent.BindMode);
    }

    [Fact]
    public void Receives_Should_NotAffectQueueBindMode_When_TypeLevelBindModeUsed()
    {
        // arrange
        var descriptor = new TestDescriptor();

        // act: per-type BindExplicitly affects only that type, not the queue
        descriptor.Receives<OrderCreated>(r => r.BindExplicitly());

        // assert: queue-level bind mode is unset; type-level intent carries Explicit
        Assert.Null(descriptor.Configuration.BindMode);
        Assert.Equal(MessagingBindMode.Explicit, descriptor.Configuration.TypeBinds[typeof(OrderCreated)].BindMode);
    }

    [Fact]
    public void Receives_Should_MergeIntoSingleTypeBindEntry_When_CalledTwiceForSameType()
    {
        // arrange
        var descriptor = new TestDescriptor();

        // act: two separate Receives<T> calls; the second one sets BindExplicitly
        descriptor.Receives<OrderCreated>(r => r.BindImplicitly());
        descriptor.Receives<OrderCreated>(r => r.BindExplicitly());

        // assert: one entry in TypeBinds; last explicit wins via merge
        Assert.Single(descriptor.Configuration.TypeBinds);
        var intent = descriptor.Configuration.TypeBinds[typeof(OrderCreated)];
        Assert.Equal(MessagingBindMode.Explicit, intent.BindMode);
    }
}
