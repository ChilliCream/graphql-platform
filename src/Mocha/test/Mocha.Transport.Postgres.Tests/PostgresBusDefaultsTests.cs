using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

public class PostgresBusDefaultsTests
{
    [Fact]
    public void Queue_AutoDelete_Should_ApplyDefault_When_NotExplicitlySet()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.ConfigureDefaults(d => d.Queue.AutoDelete = true);
            t.DeclareQueue("my-queue");
        });

        // assert
        var queue = topology.Queues.First(q => q.Name == "my-queue");
        Assert.True(queue.AutoDelete);
    }

    [Fact]
    public void Queue_AutoDelete_Should_NotOverrideExplicit_When_AlreadySet()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
            t.ConfigureDefaults(d => d.Queue.AutoDelete = true));

        // Queues added by convention that already have AutoDelete=true (e.g. reply queues)
        // should keep their setting. Normal queues should get the default.
        var normalQueues = topology.Queues.Where(q => !q.Name.StartsWith("response-")).ToList();
        foreach (var queue in normalQueues)
        {
            Assert.True(queue.AutoDelete);
        }
    }

    [Fact]
    public void Queue_AutoProvision_Should_ApplyDefault_When_Set()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.ConfigureDefaults(d => d.Queue.AutoProvision = false);
            t.DeclareQueue("no-provision");
        });

        // assert
        var queue = topology.Queues.First(q => q.Name == "no-provision");
        Assert.False(queue.AutoProvision);
    }

    [Fact]
    public void Queue_Should_UseOriginalDefaults_When_NoDefaultsConfigured()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
            t.DeclareQueue("default-queue"));

        // assert
        var queue = topology.Queues.First(q => q.Name == "default-queue");
        Assert.NotEqual(true, queue.AutoDelete);
        Assert.NotEqual(false, queue.AutoProvision);
    }

    [Fact]
    public void Topic_AutoProvision_Should_ApplyDefault_When_Set()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.ConfigureDefaults(d => d.Topic.AutoProvision = false);
            t.DeclareTopic("no-provision");
        });

        // assert
        var topic = topology.Topics.First(t => t.Name == "no-provision");
        Assert.False(topic.AutoProvision);
    }

    [Fact]
    public void Topic_Should_UseOriginalDefaults_When_NoDefaultsConfigured()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
            t.DeclareTopic("default-topic"));

        // assert
        var topic = topology.Topics.First(t => t.Name == "default-topic");
        Assert.NotEqual(false, topic.AutoProvision);
    }

    [Fact]
    public void ConfigureDefaults_Should_AccumulateSettings_When_CalledMultipleTimes()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.ConfigureDefaults(d => d.Queue.AutoDelete = true);
            t.ConfigureDefaults(d => d.Topic.AutoProvision = false);
            t.DeclareQueue("q1");
            t.DeclareTopic("t1");
        });

        // assert
        var queue = topology.Queues.First(q => q.Name == "q1");
        Assert.True(queue.AutoDelete);

        var topic = topology.Topics.First(t => t.Name == "t1");
        Assert.False(topic.AutoProvision);
    }

    [Fact]
    public void ConfigureDefaults_Should_OverridePreviousValue_When_CalledAgain()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.ConfigureDefaults(d => d.Queue.AutoDelete = true);
            t.ConfigureDefaults(d => d.Queue.AutoDelete = false);
            t.DeclareQueue("q1");
        });

        // assert
        var queue = topology.Queues.First(q => q.Name == "q1");
        Assert.False(queue.AutoDelete);
    }

    [Fact]
    public void Defaults_Should_ApplyToConventionCreatedQueues_When_HandlersRegistered()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
            t.ConfigureDefaults(d => d.Queue.AutoProvision = false));

        // assert - convention-created queues should get the default, except reply queues
        // which always have explicit AutoProvision=true since they must be created on-demand
        foreach (var queue in topology.Queues.Where(q => q.AutoDelete != true))
        {
            Assert.False(queue.AutoProvision);
        }
    }

    [Fact]
    public void Defaults_Should_ApplyToConventionCreatedTopics_When_HandlersRegistered()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
            t.ConfigureDefaults(d => d.Topic.AutoProvision = false));

        // assert - convention-created topics should also get the default
        foreach (var topic in topology.Topics)
        {
            Assert.False(topic.AutoProvision);
        }
    }

    [Fact]
    public void Defaults_Should_BeAccessibleFromTopology_When_Configured()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
            t.ConfigureDefaults(d =>
            {
                d.Queue.AutoDelete = true;
                d.Queue.AutoProvision = false;
                d.Topic.AutoProvision = false;
            }));

        // assert
        Assert.True(topology.Defaults.Queue.AutoDelete);
        Assert.False(topology.Defaults.Queue.AutoProvision);
        Assert.False(topology.Defaults.Topic.AutoProvision);
    }

    [Fact]
    public void Defaults_Should_HaveNullValues_When_NotConfigured()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(_ => { });

        // assert
        Assert.Null(topology.Defaults.Queue.AutoDelete);
        Assert.Null(topology.Defaults.Queue.AutoProvision);
        Assert.Null(topology.Defaults.Topic.AutoProvision);
    }

    [Fact]
    public void ConfigureDefaults_Should_ChainFluently_When_Called()
    {
        // arrange & act - this should compile and not throw
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.ConfigureDefaults(d => d.Queue.AutoDelete = true)
             .ConfigureDefaults(d => d.Topic.AutoProvision = false);
            t.DeclareQueue("q1");
            t.DeclareQueue("q2");
        });

        // assert
        Assert.True(topology.Defaults.Queue.AutoDelete);
        Assert.False(topology.Defaults.Topic.AutoProvision);
    }

    [Fact]
    public void Endpoint_MaxBatchSize_Should_ApplyDefault_When_Configured()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var runtime = services.AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=test;Username=test;Password=test");
                t.ConfigureDefaults(d => d.Endpoint.MaxBatchSize = 50);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        Assert.Equal(50, topology(endpoint));

        static int topology(PostgresReceiveEndpoint ep)
        {
            var field = typeof(PostgresReceiveEndpoint)
                .GetField("_maxBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)field!.GetValue(ep)!;
        }
    }

    [Fact]
    public void Endpoint_MaxConcurrency_Should_ApplyDefault_When_Configured()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var runtime = services.AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=test;Username=test;Password=test");
                t.ConfigureDefaults(d => d.Endpoint.MaxConcurrency = 32);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        Assert.Equal(32, maxConcurrency(endpoint));

        static int maxConcurrency(PostgresReceiveEndpoint ep)
        {
            var field = typeof(PostgresReceiveEndpoint)
                .GetField("_maxConcurrency", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)field!.GetValue(ep)!;
        }
    }

    [Fact]
    public void Endpoint_MaxBatchSize_Should_NotOverrideExplicit_When_SetOnEndpoint()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var runtime = services.AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=test;Username=test;Password=test");
                t.ConfigureDefaults(d => d.Endpoint.MaxBatchSize = 50);
                t.Endpoint("custom-ep")
                    .Queue("custom-q")
                    .Handler<OrderCreatedHandler>()
                    .MaxBatchSize(200);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .First(e => e.Name == "custom-ep");

        // assert - explicit 200 should not be overridden by default 50
        Assert.Equal(200, maxBatchSize(endpoint));

        static int maxBatchSize(PostgresReceiveEndpoint ep)
        {
            var field = typeof(PostgresReceiveEndpoint)
                .GetField("_maxBatchSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)field!.GetValue(ep)!;
        }
    }

    [Fact]
    public void Endpoint_Defaults_Should_HaveNullValues_When_NotConfigured()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(_ => { });

        // assert
        Assert.Null(topology.Defaults.Endpoint.MaxBatchSize);
        Assert.Null(topology.Defaults.Endpoint.MaxConcurrency);
    }
}
