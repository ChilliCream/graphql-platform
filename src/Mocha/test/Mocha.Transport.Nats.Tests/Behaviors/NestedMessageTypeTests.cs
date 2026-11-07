using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;

namespace Mocha.Transport.Nats.Tests.Behaviors;

/// <summary>
/// Verifies that nested message types sharing a name get their own subjects and consumers.
/// </summary>
[Collection(JetStreamCollection.Name)]
public sealed class NestedMessageTypeTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PublishAsync_Should_DeliverToMatchingHandler_When_NestedTypesShareName()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<CreateAccountErrorHandler>()
            .AddEventHandler<DeleteAccountErrorHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(
            new CreateAccountResponse.UnexpectedError("create failed"),
            TestContext.Current.CancellationToken);
        await messageBus.PublishAsync(
            new DeleteAccountResponse.UnexpectedError("delete failed"),
            TestContext.Current.CancellationToken);

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
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddMessageBus()
            .AddRequestHandler<CreateAccountHandler>()
            .AddRequestHandler<DeleteAccountHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;
        using var cts = new CancellationTokenSource(s_timeout);

        // act
        var created = await messageBus.RequestAsync(new CreateAccount.Request(Fail: false), cts.Token);
        var createFailed = await messageBus.RequestAsync(new CreateAccount.Request(Fail: true), cts.Token);
        var deleteFailed = await messageBus.RequestAsync(new DeleteAccount.Request(Fail: true), cts.Token);

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
    public async Task Topology_Should_UseDistinctSubjectsAndConsumers_When_NestedTypesShareName()
    {
        // arrange
        await using var scope = await fixture.CreateScopeAsync();

        // act
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<CreateAccountErrorHandler>()
            .AddEventHandler<DeleteAccountErrorHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        // assert
        var consumers = bus.Topology.Consumers
            .Where(c => c.FilterSubjects.Any(s => s.EndsWith(".unexpected-error", StringComparison.Ordinal)))
            .ToArray();
        Assert.Equal(2, consumers.Length);
        Assert.NotEqual(consumers[0].Name, consumers[1].Name);
        Assert.Empty(consumers[0].FilterSubjects.Intersect(consumers[1].FilterSubjects));
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
