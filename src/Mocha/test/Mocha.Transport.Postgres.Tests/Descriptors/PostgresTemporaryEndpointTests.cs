using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Descriptors;

/// <summary>
/// Verifies the <c>Temporary()</c> API on explicit receive endpoints and unified queues, its
/// propagation to the backing queue's auto-delete lifecycle, and conflict detection against an
/// explicit <c>AutoDelete(false)</c>.
/// </summary>
public class PostgresTemporaryEndpointTests
{
    [Fact]
    public void Temporary_Should_SetIsTemporaryAndAutoDelete_When_CalledOnExplicitEndpoint()
    {
        // arrange & act
        var (_, transport, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.BindExplicitly();
            t.Endpoint("temp-endpoint").Temporary();
        });

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Name == "temp-endpoint");
        Assert.True(endpoint.Configuration.IsTemporary);

        var queue = topology.Queues.Single(q => q.Name == "temp-endpoint");
        Assert.True(queue.AutoDelete);
    }

    [Fact]
    public void Temporary_Should_ReturnDescriptor_When_Called_ForChaining()
    {
        // arrange & act
        var (_, transport, _) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.BindExplicitly();
            t.Endpoint("chained-endpoint").Temporary().MaxConcurrency(3);
        });

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Name == "chained-endpoint");
        Assert.True(endpoint.Configuration.IsTemporary);
        Assert.Equal(3, endpoint.Configuration.MaxConcurrency);
    }

    [Fact]
    public void Temporary_Should_SetIsTemporaryAndAutoDelete_When_CalledOnUnifiedQueue()
    {
        // arrange & act
        var (_, transport, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("temp-queue").Temporary();
        });

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Queue.Name == "temp-queue");
        Assert.True(endpoint.Configuration.IsTemporary);

        var queue = topology.Queues.Single(q => q.Name == "temp-queue");
        Assert.True(queue.AutoDelete);
    }

    [Fact]
    public void Temporary_Should_LeaveAutoDeleteUnset_When_NotCalled()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.BindExplicitly();
            t.Queue("durable-queue");
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "durable-queue");
        Assert.NotEqual(true, queue.AutoDelete);
    }

    [Fact]
    public void Temporary_Should_Throw_When_UnifiedQueueDeclaresConflictingAutoDeleteFalse()
    {
        // arrange & act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PostgresBusFixture.CreateTopologyWithTransport(t =>
            {
                t.BindExplicitly();
                t.Queue("conflicting-queue").Temporary().AutoDelete(false);
            }));

        // assert
        Assert.Contains("conflicting-queue", exception.Message);
        Assert.Contains("Temporary()", exception.Message);
        Assert.Contains("AutoDelete(false)", exception.Message);
    }

    [Fact]
    public void Temporary_Should_Throw_When_ExplicitEndpointQueueDeclaresConflictingAutoDeleteFalse()
    {
        // arrange & act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PostgresBusFixture.CreateTopologyWithTransport(t =>
            {
                t.BindExplicitly();
                t.Endpoint("conflicting-endpoint").Temporary();
                t.DeclareQueue("conflicting-endpoint").AutoDelete(false);
            }));

        // assert
        Assert.Contains("conflicting-endpoint", exception.Message);
        Assert.Contains("Temporary()", exception.Message);
        Assert.Contains("AutoDelete(false)", exception.Message);
    }

    [Fact]
    public void Temporary_Should_SetAutoDelete_When_QueueDeclaredSeparately()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.BindExplicitly();
            t.Endpoint("x").Temporary();
            t.DeclareQueue("x");
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "x");
        Assert.True(queue.AutoDelete);
    }

    [Fact]
    public void Temporary_Should_BeImpliedByReplyKind_When_RequestReplyConfigured()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // assert - the reply endpoint's queue is auto-deleted via the shared IsTemporary flag
        var replyEndpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Kind == ReceiveEndpointKind.Reply);
        var replyQueue = topology.Queues.Single(q => q.Name == replyEndpoint.Queue.Name);
        Assert.True(replyQueue.AutoDelete);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddPostgres(t => t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test"))
            .BuildRuntime();
        return runtime;
    }
}
