using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Transport.InMemory;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Tests the reply leg of a saga send whose command has no response type, so it is handled by
/// <c>SendConsumer</c> rather than the request consumer.
/// </summary>
public class SagaSendCommandTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    private static readonly TaskCompletionSource<NotAcknowledgedEvent> s_faultObserved = new();

    private static readonly TaskCompletionSource<object> s_replyObserved = new();

    private static async Task<ServiceProvider> CreateBusAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    [Fact]
    public async Task Saga_Should_ReceiveFault_When_CommandWithoutResponseFaults()
    {
        // arrange
        var handlerRan = new TaskCompletionSource();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(handlerRan);
            b.AddRequestHandler<FaultingCommandHandler>();
            b.AddSaga<FaultingCommandSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartFaultingCommandEvent(), CancellationToken.None);

        // assert - the handler ran and threw, proving the command was delivered
        await handlerRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert - the fault reply routed back to the saga
        var fault = await s_faultObserved.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.Equal(ErrorCodes.Exception, fault.ErrorCode);
    }

    [Fact]
    public async Task Saga_Should_ReceiveAcknowledgement_When_CommandWithoutResponseSucceeds()
    {
        // A command with no response type still acknowledges, so the saga's reply route sees an
        // AcknowledgedEvent rather than nothing at all.

        // arrange
        var handlerRan = new TaskCompletionSource();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(handlerRan);
            b.AddRequestHandler<SucceedingCommandHandler>();
            b.AddSaga<SucceedingCommandSaga>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new StartSucceedingCommandEvent(), CancellationToken.None);

        // assert - the handler ran and returned
        await handlerRan.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert - the acknowledgement routed back to the saga
        var reply = await s_replyObserved.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        Assert.IsType<AcknowledgedEvent>(reply);
    }

    public sealed class CommandState : SagaStateBase;

    public sealed class StartFaultingCommandEvent;

    public sealed class StartSucceedingCommandEvent;

    public sealed record FaultingCommand;

    public sealed record SucceedingCommand;

    public sealed class FaultingCommandHandler(TaskCompletionSource handlerRan)
        : IEventRequestHandler<FaultingCommand>
    {
        public ValueTask HandleAsync(FaultingCommand request, CancellationToken cancellationToken)
        {
            handlerRan.TrySetResult();
            throw new InvalidOperationException("terminal failure");
        }
    }

    public sealed class SucceedingCommandHandler(TaskCompletionSource handlerRan)
        : IEventRequestHandler<SucceedingCommand>
    {
        public ValueTask HandleAsync(SucceedingCommand request, CancellationToken cancellationToken)
        {
            handlerRan.TrySetResult();
            return default;
        }
    }

    public sealed class FaultingCommandSaga : Saga<CommandState>
    {
        protected override void Configure(ISagaDescriptor<CommandState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartFaultingCommandEvent>()
                .StateFactory(_ => new CommandState())
                .Send((_, _) => new FaultingCommand())
                .TransitionTo("Awaiting");

            descriptor
                .During("Awaiting")
                .OnFault()
                .Then((_, fault) => s_faultObserved.TrySetResult(fault))
                .TransitionTo("Failed");

            descriptor.Finally("Failed");
        }
    }

    public sealed class SucceedingCommandSaga : Saga<CommandState>
    {
        protected override void Configure(ISagaDescriptor<CommandState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StartSucceedingCommandEvent>()
                .StateFactory(_ => new CommandState())
                .Send((_, _) => new SucceedingCommand())
                .TransitionTo("Awaiting");

            descriptor
                .During("Awaiting")
                .OnAnyReply()
                .Then((_, reply) => s_replyObserved.TrySetResult(reply))
                .TransitionTo("Done");

            descriptor.During("Awaiting").OnFault().TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }
}
