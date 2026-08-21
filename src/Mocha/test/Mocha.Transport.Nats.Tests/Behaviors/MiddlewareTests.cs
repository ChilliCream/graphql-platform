using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Middlewares;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

public sealed record TransportScoped(string Id);

public sealed record EndpointScoped(string Id);

[Collection(JetStreamCollection.Name)]
public class MiddlewareTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task UseReceive_Should_RunBeforeTheHandler_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();

        var builder = CreateBuilder(tracker, recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<TransportScopedHandler>()
            .AddNats(nats => nats
                .StreamName("mw-transport")
                .UseReceive(Track(tracker, "transport-receive")));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new TransportScoped("T-1"), cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler never ran.");
            Assert.Equal(["transport-receive", "handler"], tracker.Steps);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task UseReceive_Should_RunBeforeTheHandler_When_ConfiguredOnTheEndpoint()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();

        var builder = CreateBuilder(tracker, recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<EndpointScopedHandler>()
            .AddNats(nats => nats
                .StreamName("mw-endpoint")
                .Endpoint("mw-ep")
                .Handler<EndpointScopedHandler>()
                .UseReceive(Track(tracker, "endpoint-receive")));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new EndpointScoped("E-1"), cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler never ran.");
            Assert.Equal(["endpoint-receive", "handler"], tracker.Steps);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    private static ReceiveMiddlewareConfiguration Track(MiddlewareTracker tracker, string step)
        => new(
            (_, next) => async context =>
            {
                tracker.Add(step);
                await next(context);
            },
            step);

    private HostApplicationBuilder CreateBuilder(MiddlewareTracker tracker, MessageRecorder recorder)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(tracker);
        builder.Services.AddSingleton(recorder);

        return builder;
    }

    public sealed class MiddlewareTracker
    {
        private readonly ConcurrentQueue<string> _steps = new();

        public IReadOnlyList<string> Steps => [.. _steps];

        public void Add(string step) => _steps.Enqueue(step);
    }

    public sealed class TransportScopedHandler(MiddlewareTracker tracker, MessageRecorder recorder)
        : IEventHandler<TransportScoped>
    {
        public ValueTask HandleAsync(TransportScoped message, CancellationToken cancellationToken)
        {
            tracker.Add("handler");
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class EndpointScopedHandler(MiddlewareTracker tracker, MessageRecorder recorder)
        : IEventHandler<EndpointScoped>
    {
        public ValueTask HandleAsync(EndpointScoped message, CancellationToken cancellationToken)
        {
            tracker.Add("handler");
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
