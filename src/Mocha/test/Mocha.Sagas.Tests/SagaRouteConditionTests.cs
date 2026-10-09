using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests for the routes that <see cref="SagaConsumer"/> derives. Reply transitions are gated on the
/// saga-id header so non saga replies on the shared reply endpoint cannot select a saga; catch-all
/// replies exclude faults, typed replies keep their message type term, subscribe transitions route by
/// type alone, and reply routes are bound to the saga's own reply endpoint.
/// </summary>
public class SagaRouteConditionTests
{
    [Fact]
    public void Configure_Should_GateOnSagaIdAndExcludeFaults_When_OnAnyReply()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddSaga<AnyReplySaga>());

        // assert
        var route = GetSagaRoute(runtime, InboundRouteKind.Reply, typeof(object));
        var description = route.Condition.Describe();
        Assert.Equal("And", description.Kind);
        Assert.Collection(
            description.Children,
            c => Assert.Equal("HeaderPresent", c.Kind),
            c => Assert.Equal("NotFault", c.Kind));
    }

    [Fact]
    public void Configure_Should_GateOnSagaIdAndMessageType_When_OnFault()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddSaga<AnyReplySaga>());

        // assert - a fault is a typed reply, so it keeps the message type term next to the saga-id gate
        var route = GetSagaRoute(runtime, InboundRouteKind.Reply, typeof(NotAcknowledgedEvent));
        var description = route.Condition.Describe();
        Assert.Equal("And", description.Kind);
        Assert.Collection(
            description.Children,
            c =>
            {
                Assert.Equal("HeaderPresent", c.Kind);
                Assert.Equal("saga-id", c.Detail);
            },
            c => Assert.Equal("MessageType", c.Kind));
    }

    [Fact]
    public void Configure_Should_KeepMessageTypeTerm_When_TypedOnReply()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddSaga<TypedReplySaga>());

        // assert - a typed reply keeps its message type term in addition to the saga-id gate
        var route = GetSagaRoute(runtime, InboundRouteKind.Reply);
        var description = route.Condition.Describe();
        Assert.Equal("And", description.Kind);
        Assert.Collection(
            description.Children,
            c =>
            {
                Assert.Equal("HeaderPresent", c.Kind);
                Assert.Equal("saga-id", c.Detail);
            },
            c => Assert.Equal("MessageType", c.Kind));
    }

    [Fact]
    public void Configure_Should_DeriveMessageTypeCondition_When_SubscribeTransition()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddSaga<AnyReplySaga>());

        // assert - the start event route is not saga-id gated, it routes by type alone
        var route = GetSagaRoute(runtime, InboundRouteKind.Subscribe);
        Assert.IsType<MessageTypeCondition>(route.Condition);
    }

    [Fact]
    public void Configure_Should_BindReplyRoutesToOwnReplyEndpoint_When_SagasHaveReplyTransitions()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddSaga<AnyReplySaga>();
            b.AddSaga<TypedReplySaga>();
        });

        // assert
        runtime
            .Consumers.OfType<SagaConsumer>()
            .SelectMany(runtime.Router.GetInboundByConsumer)
            .Select(r => $"{r.Consumer!.Name} {r.Kind} {r.MessageType?.RuntimeType.Name}"
                + $" -> {r.Endpoint!.Name} ({r.Endpoint.Kind})")
            .Order(StringComparer.Ordinal)
            .ToArray()
            .MatchInlineSnapshot(
                """
                [
                  "any-reply-saga Reply NotAcknowledgedEvent -> Replies (Reply)",
                  "any-reply-saga Reply NotAcknowledgedEvent -> mocha.sagas.tests.any-reply-saga_reply (Reply)",
                  "any-reply-saga Reply Object -> Replies (Reply)",
                  "any-reply-saga Reply Object -> mocha.sagas.tests.any-reply-saga_reply (Reply)",
                  "any-reply-saga Subscribe StartEvent -> mocha.sagas.tests.any-reply-saga (Default)",
                  "typed-reply-saga Reply Response -> Replies (Reply)",
                  "typed-reply-saga Reply Response -> mocha.sagas.tests.typed-reply-saga_reply (Reply)",
                  "typed-reply-saga Subscribe StartEvent -> mocha.sagas.tests.typed-reply-saga (Default)"
                ]
                """);
    }

    private static InboundRoute GetSagaRoute(
        MessagingRuntime runtime,
        InboundRouteKind kind,
        Type? messageType = null)
    {
        var consumer = runtime.Consumers.OfType<SagaConsumer>().Single();
        return runtime
            .Router.GetInboundByConsumer(consumer)
            .Single(r => r.Kind == kind
                && r.Endpoint != r.Endpoint?.Transport.ReplyReceiveEndpoint
                && (messageType is null || r.MessageType?.RuntimeType == messageType));
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    public sealed class ReplyState : SagaStateBase;

    public sealed class StartEvent;

    public sealed record Response;

    public sealed record Request : IEventRequest<Response>;

    /// <summary>
    /// A saga that sends a request and finalizes on any reply (OnReply&lt;object&gt;).
    /// </summary>
    public sealed class AnyReplySaga : Saga<ReplyState>
    {
        protected override void Configure(ISagaDescriptor<ReplyState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartEvent>()
                .StateFactory(_ => new ReplyState())
                .Send((_, _) => new Request())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnAnyReply().TransitionTo("Done");
            descriptor.During("Awaiting").OnFault().TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }

    /// <summary>
    /// A saga that sends a request and finalizes on a typed reply (OnReply&lt;Response&gt;).
    /// </summary>
    public sealed class TypedReplySaga : Saga<ReplyState>
    {
        protected override void Configure(ISagaDescriptor<ReplyState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartEvent>()
                .StateFactory(_ => new ReplyState())
                .Send((_, _) => new Request())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnReply<Response>().TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }
}
