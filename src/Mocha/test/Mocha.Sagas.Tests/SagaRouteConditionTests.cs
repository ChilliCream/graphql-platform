using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests for the route conditions that <see cref="SagaConsumer"/> derives. Reply transitions are
/// gated on the saga-id header so non saga replies on the shared reply endpoint cannot select a saga;
/// typed replies additionally keep their message type term, while subscribe transitions route by type
/// alone.
/// </summary>
public class SagaRouteConditionTests
{
    [Fact]
    public void Configure_Should_GateOnSagaIdOnly_When_OnAnyReply()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddSaga<AnyReplySaga>());

        // assert - OnAnyReply routes every saga-id reply to the consumer, so it gates on the saga-id alone
        var route = GetSagaRoute(runtime, InboundRouteKind.Reply);
        var description = route.Condition.Describe();
        Assert.Equal("HeaderPresent", description.Kind);
        Assert.Equal("saga-id", description.Detail);
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

    private static InboundRoute GetSagaRoute(MessagingRuntime runtime, InboundRouteKind kind)
    {
        var consumer = runtime.Consumers.OfType<SagaConsumer>().Single();
        return runtime.Router.GetInboundByConsumer(consumer).Single(r => r.Kind == kind);
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
