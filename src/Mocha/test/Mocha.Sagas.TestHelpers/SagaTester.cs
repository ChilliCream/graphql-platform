using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

public static class SagaTester
{
    private static readonly IMessagingRuntime s_runtime = CreateRuntime();

    internal static IMessagingRuntime Runtime => s_runtime;

    public static SagaTester<T> Create<T>(Saga<T> saga) where T : SagaStateBase
    {
        saga.Initialize(TestMessagingSetupContext.Instance);
        return new SagaTester<T>(saga);
    }

    public static class Defaults
    {
        public static readonly Guid CorrelationId = Guid.Parse("7A921D31-B758-4CC8-B849-296669B97E41");

        public static string ReplyEndpoint = "ReplyEndpoint";
    }

    private static IMessagingRuntime CreateRuntime()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMessagingRuntime>();
    }
}

public sealed class SagaTester<T> where T : SagaStateBase
{
    public Saga<T> Saga { get; }

    public TestMessageOutbox Outbox { get; } = new();

    public TestSagaStore Store { get; } = new();

    public TestSagaCleanup Cleanup { get; } = new();

    public T? State => Store.States.OfType<T>().FirstOrDefault();

    private readonly IServiceProvider _services;

    internal SagaTester(Saga<T> saga)
    {
        Saga = saga;
        _services = new ServiceCollection()
            .AddSingleton<ISagaCleanup>(Cleanup)
            .AddSingleton<IMessageBus>(new TestMessageBus(Outbox))
            .BuildServiceProvider();
    }

    public void SetState(T state)
    {
        Store.States.Add(state);
    }

    public async Task ExecuteAsync(object @event)
    {
        var context = new TestConsumeContext
        {
            Runtime = SagaTester.Runtime,
            Services = _services,
            CancellationToken = CancellationToken.None,
            CorrelationId = Guid.NewGuid().ToString(),
            MessageId = Guid.NewGuid().ToString(),
            ResponseAddress = new Uri($"queue://test/{SagaTester.Defaults.ReplyEndpoint}")
        };

        context.Features.GetOrSet<MessageParsingFeature>().Message = @event;
        context.Features.GetOrSet<SagaFeature>().Store = Store;

        if (State != null)
        {
            context.MutableHeaders.Set(SagaContextData.SagaId, State.Id.ToString("D"));
        }

        await Saga.HandleEvent(context);
    }

    public T ExpectState(string state)
    {
        Assert.Equal(state, State!.State);

        return State;
    }

    public void ExpectCompleted()
    {
        Assert.Empty(Store.States);
    }

    public TMessage ExpectSentMessage<TMessage>()
    {
        var message = Outbox.Messages.LastOrDefault(x => x.Message is TMessage);
        Assert.NotNull(message);
        Assert.IsType<SendOptions>(message.Options);
        return Assert.IsType<TMessage>(message.Message);
    }

    public TMessage ExpectReplyMessage<TMessage>()
    {
        var message = Outbox.Messages.LastOrDefault(x => x.Message is TMessage);
        Assert.NotNull(message);
        Assert.IsType<ReplyOptions>(message.Options);
        return Assert.IsType<TMessage>(message.Message);
    }

    public TMessage ExpectPublishedMessage<TMessage>()
    {
        var message = Outbox.Messages.LastOrDefault(x => x.Message is TMessage);
        Assert.NotNull(message);
        Assert.IsType<PublishOptions>(message.Options);
        return Assert.IsType<TMessage>(message.Message);
    }

    public SendOptions ExpectSendOptions<TMessage>(TMessage message) where TMessage : class
    {
        var messageOutbox = Assert.Single(Outbox.Messages, x => x.Message == message);
        return Assert.IsType<SendOptions>(messageOutbox.Options);
    }

    public void ExpectNoOptions<TMessage>(TMessage message) where TMessage : class
    {
        var messageOutbox = Assert.Single(Outbox.Messages, x => x.Message == message);
        Assert.Null(messageOutbox.Options);
    }
}
