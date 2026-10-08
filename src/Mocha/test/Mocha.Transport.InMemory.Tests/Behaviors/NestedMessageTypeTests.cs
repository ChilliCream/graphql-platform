using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class NestedMessageTypeTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PublishAsync_Should_DeliverToMatchingHandler_When_NestedTypesShareName()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<CreateAccountErrorHandler>()
            .AddEventHandler<DeleteAccountErrorHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        using var cts = new CancellationTokenSource(s_timeout);
        await bus.PublishAsync(new CreateAccountResponse.UnexpectedError("create failed"), cts.Token);
        await bus.PublishAsync(new DeleteAccountResponse.UnexpectedError("delete failed"), cts.Token);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout, expectedCount: 2));
        recorder.Messages.Cast<string>().Order(StringComparer.Ordinal).MatchInlineSnapshots(
        [
            "CreateAccountErrorHandler received CreateAccountResponse.UnexpectedError (create failed)",
            "DeleteAccountErrorHandler received DeleteAccountResponse.UnexpectedError (delete failed)"
        ]);
    }

    [Fact]
    public async Task RequestAsync_Should_ReturnMatchingNestedResponse_When_NestedTypesShareName()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<CreateAccountHandler>()
            .AddRequestHandler<DeleteAccountHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        using var cts = new CancellationTokenSource(s_timeout);
        var created = await bus.RequestAsync(new CreateAccount.Request(Fail: false), cts.Token);
        var createFailed = await bus.RequestAsync(new CreateAccount.Request(Fail: true), cts.Token);
        var deleteFailed = await bus.RequestAsync(new DeleteAccount.Request(Fail: true), cts.Token);

        // assert
        new object[] { created, createFailed, deleteFailed }
            .Select(Describe)
            .MatchInlineSnapshots(
            [
                "CreateAccountResponse.AccountCreated",
                "CreateAccountResponse.UnexpectedError (create failed)",
                "DeleteAccountResponse.UnexpectedError (delete failed)"
            ]);
    }

    [Fact]
    public void Topology_Should_UseDistinctTopicsAndQueues_When_NestedTypesShareName()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.Host(h => h.ServiceName("test-app"));
        builder.AddEventHandler<CreateAccountErrorHandler>();
        builder.AddEventHandler<DeleteAccountErrorHandler>();
        builder.AddRequestHandler<CreateAccountHandler>();
        builder.AddRequestHandler<DeleteAccountHandler>();

        // act
        var runtime = builder.AddInMemory().BuildRuntime();

        // assert
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var snapshot = TopologySnapshotHelper.CreateSnapshot((InMemoryMessagingTopology)transport.Topology);
        snapshot.MatchSnapshot();
    }

    private static string Describe(object message)
        => message switch
        {
            CreateAccountResponse.UnexpectedError e => $"{Name(message.GetType())} ({e.Message})",
            DeleteAccountResponse.UnexpectedError e => $"{Name(message.GetType())} ({e.Message})",
            _ => Name(message.GetType())
        };

    private static string Name(Type type) => $"{type.DeclaringType!.Name}.{type.Name}";

    public abstract record CreateAccountResponse
    {
        public sealed record AccountCreated(Guid Id) : CreateAccountResponse;

        public sealed record UnexpectedError(string Message) : CreateAccountResponse;
    }

    public abstract record DeleteAccountResponse
    {
        public sealed record AccountDeleted : DeleteAccountResponse;

        public sealed record UnexpectedError(string Message) : DeleteAccountResponse;
    }

    public static class CreateAccount
    {
        public sealed record Request(bool Fail) : IEventRequest<CreateAccountResponse>;
    }

    public static class DeleteAccount
    {
        public sealed record Request(bool Fail) : IEventRequest<DeleteAccountResponse>;
    }

    public sealed class CreateAccountErrorHandler(MessageRecorder recorder)
        : IEventHandler<CreateAccountResponse.UnexpectedError>
    {
        public ValueTask HandleAsync(CreateAccountResponse.UnexpectedError message, CancellationToken cancellationToken)
        {
            recorder.Record($"CreateAccountErrorHandler received {Describe(message)}");
            return default;
        }
    }

    public sealed class DeleteAccountErrorHandler(MessageRecorder recorder)
        : IEventHandler<DeleteAccountResponse.UnexpectedError>
    {
        public ValueTask HandleAsync(DeleteAccountResponse.UnexpectedError message, CancellationToken cancellationToken)
        {
            recorder.Record($"DeleteAccountErrorHandler received {Describe(message)}");
            return default;
        }
    }

    public sealed class CreateAccountHandler : IEventRequestHandler<CreateAccount.Request, CreateAccountResponse>
    {
        public ValueTask<CreateAccountResponse> HandleAsync(
            CreateAccount.Request request,
            CancellationToken cancellationToken)
            => ValueTask.FromResult<CreateAccountResponse>(
                request.Fail
                    ? new CreateAccountResponse.UnexpectedError("create failed")
                    : new CreateAccountResponse.AccountCreated(Guid.NewGuid()));
    }

    public sealed class DeleteAccountHandler : IEventRequestHandler<DeleteAccount.Request, DeleteAccountResponse>
    {
        public ValueTask<DeleteAccountResponse> HandleAsync(
            DeleteAccount.Request request,
            CancellationToken cancellationToken)
            => ValueTask.FromResult<DeleteAccountResponse>(
                request.Fail
                    ? new DeleteAccountResponse.UnexpectedError("delete failed")
                    : new DeleteAccountResponse.AccountDeleted());
    }
}
