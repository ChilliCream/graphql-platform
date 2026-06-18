using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Descriptors;

public class PostgresDescriptorTests
{
    [Fact]
    public void Transport_Should_UseCustomSchema_When_SchemaConfigured()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t => t.Schema("custom"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        Assert.Equal("custom", transport.Schema);
    }

    [Fact]
    public void Transport_Should_UseCustomName_When_NameConfigured()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t => t.Name("my-transport"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        Assert.Equal("my-transport", transport.Name);
    }

    [Fact]
    public void Transport_Should_BeDefault_When_IsDefaultTransportCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t => t.IsDefaultTransport());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        Assert.NotNull(transport);
        Assert.Single(runtime.Transports);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetQueue_When_ToQueueCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("my-q");
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.IsType<PostgresQueue>(endpoint.Destination);
        Assert.Equal("my-q", ((PostgresQueue)endpoint.Destination).Name);
        Assert.NotNull(endpoint.Queue);
        Assert.Equal("my-q", endpoint.Queue!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetTopic_When_ToTopicCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-t");
            t.DispatchEndpoint("ep").ToTopic("my-t");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.IsType<PostgresTopic>(endpoint.Destination);
        Assert.Equal("my-t", ((PostgresTopic)endpoint.Destination).Name);
        Assert.NotNull(endpoint.Topic);
        Assert.Equal("my-t", endpoint.Topic!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_RegisterSendRoute_When_SendCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("q");
            t.DispatchEndpoint("ep").ToQueue("q").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.IsType<PostgresQueue>(endpoint.Destination);
        Assert.Equal("q", endpoint.Queue!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_RegisterPublishRoute_When_PublishCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("t");
            t.DispatchEndpoint("ep").ToTopic("t").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "ep");
        Assert.IsType<PostgresTopic>(endpoint.Destination);
        Assert.Equal("t", endpoint.Topic!.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetQueueName_When_QueueCalled()
    {
        // arrange & act
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.Queue("custom-q").Handler<OrderCreatedHandler>().Kind(ReceiveEndpointKind.Error);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Queue.Name == "custom-q");
        Assert.Equal("custom-q", endpoint.Queue.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetKind_When_KindCalled()
    {
        // arrange & act
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.Queue("err-q").Handler<OrderCreatedHandler>().Kind(ReceiveEndpointKind.Error);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Queue.Name == "err-q");
        Assert.Equal(ReceiveEndpointKind.Error, endpoint.Kind);
    }

    [Fact]
    public void ReceiveEndpoint_Should_StoreVerbatimName_When_ErrorQueueNamedWithPascalCase()
    {
        // arrange & act
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.Queue("q").ErrorQueue("Legacy.Orders.V2_error").Handler<OrderCreatedHandler>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Queue.Name == "q");
        var feature = endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>();
        Assert.Equal("postgres:q/Legacy.Orders.V2_error", feature?.Address?.OriginalString);
        Assert.False(feature?.IsDisabled ?? false);
    }

    [Fact]
    public void ReceiveEndpoint_Should_UseConfiguredSchema_When_ErrorQueueConfiguredBeforeSchema()
    {
        // arrange & act
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.Queue("q").ErrorQueue("q_error").Handler<OrderCreatedHandler>();
                t.Schema("custom-postgres");
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Queue.Name == "q");
        Assert.Equal(
            "custom-postgres:q/q_error",
            endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>()?.Address?.OriginalString);
        Assert.Equal(
            "q_error",
            ((PostgresQueue)endpoint.Features.Get<ReceiveFaultEndpointFeature>()!.Endpoint!.Destination).Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_PreserveLaterFaultEndpoint_When_ErrorQueueConfiguredFirst()
    {
        // arrange & act
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.Queue("q").ErrorQueue("q_error").Handler<OrderCreatedHandler>();
                t.Endpoint("q").FaultEndpoint("postgres:q/other_error");
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Queue.Name == "q");
        Assert.Equal(
            "postgres:q/other_error",
            endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>()?.Address?.OriginalString);
        Assert.Equal(
            "other_error",
            ((PostgresQueue)endpoint.Features.Get<ReceiveFaultEndpointFeature>()!.Endpoint!.Destination).Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetDisableFlag_When_DisableErrorQueueCalled()
    {
        // arrange & act
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.Queue("q").DisableErrorQueue().Handler<OrderCreatedHandler>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Queue.Name == "q");
        var feature = endpoint.Configuration.Features.Get<ReceiveFaultEndpointFeature>();
        Assert.True(feature?.IsDisabled);
        Assert.Null(feature?.Address);
    }

    [Fact]
    public void Transport_Should_DefaultBindModeImplicit_When_NotConfigured()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t => { });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        Assert.Equal(MessagingBindMode.Implicit, transport.BindMode);
    }

    [Fact]
    public void Transport_Should_SetBindModeExplicit_When_BindExplicitlyCalled()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        Assert.Equal(MessagingBindMode.Explicit, transport.BindMode);
    }
}
