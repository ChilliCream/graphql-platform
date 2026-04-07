using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

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
        Assert.NotNull(receiveEndpoint.ErrorEndpoint);
        Assert.Contains("_error", receiveEndpoint.ErrorEndpoint!.Name);
    }

    [Fact]
    public void Convention_Should_SetSkippedEndpoint_When_DefaultEndpointCreated()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Kind == ReceiveEndpointKind.Default);

        // assert
        Assert.NotNull(receiveEndpoint.SkippedEndpoint);
        Assert.Contains("_skipped", receiveEndpoint.SkippedEndpoint!.Name);
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
            Assert.Null(replyEndpoint.ErrorEndpoint);
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
        Assert.NotNull(receiveEndpoint.ErrorEndpoint);
        var errorDest = receiveEndpoint.ErrorEndpoint!.Destination;
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
        Assert.NotNull(receiveEndpoint.SkippedEndpoint);
        var skippedDest = receiveEndpoint.SkippedEndpoint!.Destination;
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
                t.DeclareQueue("my-q");
                t.DeclareQueue("custom-error-q");
                t.Endpoint("ep")
                    .Queue("my-q")
                    .Handler<OrderCreatedHandler>()
                    .FaultEndpoint("postgres:///q/custom-error-q");
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var receiveEndpoint = transport.ReceiveEndpoints.First(e => e.Name == "ep");

        // assert
        Assert.NotNull(receiveEndpoint.ErrorEndpoint);
        Assert.IsType<PostgresQueue>(receiveEndpoint.ErrorEndpoint!.Destination);
        Assert.Equal("custom-error-q", ((PostgresQueue)receiveEndpoint.ErrorEndpoint.Destination).Name);
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
