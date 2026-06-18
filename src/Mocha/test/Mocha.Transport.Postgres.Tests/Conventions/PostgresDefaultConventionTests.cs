using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

using Mocha.Features;

namespace Mocha.Transport.Postgres.Tests.Conventions;

public class PostgresDefaultConventionTests
{
    [Fact]
    public void Convention_Should_SetErrorEndpoint_When_DefaultEndpointCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        var feature = receiveEndpoint.Features.Get<ReceiveFaultEndpointFeature>();
        Assert.NotNull(feature?.Endpoint);
        Assert.Contains("_error", feature.Endpoint!.Name);
    }

    [Fact]
    public void Convention_Should_SetSkippedEndpoint_When_DefaultEndpointCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        var feature = receiveEndpoint.Features.Get<ReceiveSkippedEndpointFeature>();
        Assert.NotNull(feature?.Endpoint);
        Assert.Contains("_skipped", feature.Endpoint!.Name);
    }

    [Fact]
    public void Convention_Should_NotSetErrorEndpoint_When_ReplyEndpointCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
            b.AddRequestHandler<GetOrderStatusHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        var replyEndpoint = transport.ReceiveEndpoints.FirstOrDefault(e => e.Kind == ReceiveEndpointKind.Reply);

        // assert - reply endpoints should not get auto error/skipped endpoints
        if (replyEndpoint is not null)
        {
            Assert.Null(replyEndpoint.Features.Get<ReceiveFaultEndpointFeature>()?.Endpoint);
        }
    }

    [Fact]
    public void Convention_Should_UseQueueNameInErrorEndpoint_When_QueueNameSet()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        var errorEndpoint = receiveEndpoint.Features.Get<ReceiveFaultEndpointFeature>()?.Endpoint;
        Assert.NotNull(errorEndpoint);
        var errorDest = errorEndpoint!.Destination;
        Assert.IsType<PostgresQueue>(errorDest);
        Assert.EndsWith("_error", ((PostgresQueue)errorDest).Name);
    }

    [Fact]
    public void Convention_Should_UseQueueNameInSkippedEndpoint_When_QueueNameSet()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        var skippedEndpoint = receiveEndpoint.Features.Get<ReceiveSkippedEndpointFeature>()?.Endpoint;
        Assert.NotNull(skippedEndpoint);
        var skippedDest = skippedEndpoint!.Destination;
        Assert.IsType<PostgresQueue>(skippedDest);
        Assert.EndsWith("_skipped", ((PostgresQueue)skippedDest).Name);
    }

    [Fact]
    public void Convention_Should_NotOverrideExplicitErrorEndpoint_When_UserConfigured()
    {
        // arrange & act
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=test;Username=test;Password=test");
                t.DeclareQueue("custom-error-q");
                t.Queue("my-q")
                    .Handler<OrderCreatedHandler>()
                    .FaultEndpoint("postgres:///q/custom-error-q");
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Name == "my-q");

        // assert
        var errorEndpoint = receiveEndpoint.Features.Get<ReceiveFaultEndpointFeature>()?.Endpoint;
        Assert.NotNull(errorEndpoint);
        Assert.IsType<PostgresQueue>(errorEndpoint!.Destination);
        Assert.Equal("custom-error-q", ((PostgresQueue)errorEndpoint.Destination).Name);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddPostgres(t => t.ConnectionString("Host=localhost;Database=test;Username=test;Password=test"))
            .BuildRuntime();
        return runtime;
    }
}
