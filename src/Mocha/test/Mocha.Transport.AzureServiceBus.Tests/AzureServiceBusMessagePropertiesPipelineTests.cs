using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests;

public class AzureServiceBusMessagePropertiesPipelineTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=a2V5";

    [Theory]
    [InlineData(null)]
    [InlineData("Serialization")]
    public async Task ExecuteAsync_Should_ExtractSessionIdFromNewMessage_When_BusMiddlewareReplacesMessage(
        string? before)
    {
        // arrange
        var replace = new DispatchMiddlewareConfiguration(
            static (_, next) => context =>
            {
                context.Message = new SessionEvent { SessionKey = "session-new" };
                return next(context);
            },
            "ReplaceMessage");

        // act
        var sessionId = await DispatchAsync(
            new SessionEvent { SessionKey = "session-original" },
            bus => bus.UseDispatch(replace, before: before));

        // assert
        Assert.Equal("session-new", sessionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Serialization")]
    public async Task ExecuteAsync_Should_ExtractSessionIdFromChangedMessage_When_BusMiddlewareMutatesMessage(
        string? before)
    {
        // arrange
        var mutate = new DispatchMiddlewareConfiguration(
            static (_, next) => context =>
            {
                ((SessionEvent)context.Message!).SessionKey = "session-new";
                return next(context);
            },
            "MutateMessage");

        // act
        var sessionId = await DispatchAsync(
            new SessionEvent { SessionKey = "session-original" },
            bus => bus.UseDispatch(mutate, before: before));

        // assert
        Assert.Equal("session-new", sessionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Serialization")]
    public async Task ExecuteAsync_Should_KeepSessionId_When_BusMiddlewareSetsSessionIdHeader(string? before)
    {
        // arrange
        var setHeader = new DispatchMiddlewareConfiguration(
            static (_, next) => context =>
            {
                context.Headers.Set(AzureServiceBusMessageHeaders.SessionId, "from-middleware");
                return next(context);
            },
            "SetSessionId");

        // act
        var sessionId = await DispatchAsync(
            new SessionEvent { SessionKey = "session-original" },
            bus => bus.UseDispatch(setHeader, before: before));

        // assert
        Assert.Equal("from-middleware", sessionId);
    }

    private static async Task<object?> DispatchAsync(SessionEvent message, Action<IMessageBusBuilder> configureBus)
    {
        object? sessionId = null;
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddMessage<SessionEvent>(m =>
        {
            m.UseAzureServiceBusSessionId<SessionEvent>(e => e.SessionKey);
            m.Publish(route => route.ToAzureServiceBusTopic("sessions"));
        });
        builder.ConfigureMessageBus(bus =>
        {
            bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, _) => context =>
                    {
                        sessionId = context.Envelope!.Headers!.GetValue(AzureServiceBusMessageHeaders.SessionId);
                        return default;
                    },
                    "CaptureSessionId"),
                after: DispatchMiddlewares.Serialization.Key);
            configureBus(bus);
        });
        await using var runtime = builder.AddAzureServiceBus(FakeConnectionString).BuildRuntime();
        var messageType = runtime.GetMessageType(typeof(SessionEvent));
        var endpoint = runtime.GetPublishEndpoint(messageType);
        var context = new DispatchContext { Message = message };
        context.Initialize(runtime.Services, endpoint, runtime, messageType, CancellationToken.None);

        try
        {
            await endpoint.ExecuteAsync(context);
        }
        finally
        {
            context.Reset();
        }

        return sessionId;
    }

    public sealed class SessionEvent
    {
        public required string SessionKey { get; set; }
    }
}
