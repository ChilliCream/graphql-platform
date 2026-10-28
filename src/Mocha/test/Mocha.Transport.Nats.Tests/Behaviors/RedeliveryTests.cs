using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class RedeliveryTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Handler_Should_StopBeingRedelivered_When_TheMessageHasBeenDeadLettered()
    {
        // arrange
        var counter = new InvocationCounter();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(counter)
            .AddMessageBus()
            .AddEventHandler<AlwaysFailingHandler>()
            .AddResilience(policy => policy.Default().Retry(1).ThenDeadLetter())
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new UnhandleableMessage("U-1"), CancellationToken.None);

        // Two attempts are expected: the first delivery and the one retry.
        Assert.True(
            await WaitForAtLeastAsync(counter, 2, TimeSpan.FromSeconds(30)),
            $"The handler ran {counter.Count} times, expected at least 2.");

        // Long enough for another redelivery to arrive if the message were left unsettled.
        await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken.None);

        // assert
        Assert.Equal(2, counter.Count);
    }

    [Fact]
    public async Task Redelivery_Should_RedeliverTheMessage_When_TheHandlerFailsOnce()
    {
        // arrange
        var counter = new InvocationCounter();
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(counter)
            .AddSingleton(recorder)
            .AddSingleton(new AttemptLog())
            .AddMessageBus()
            .AddEventHandler<FailOnceHandler>()
            .AddResilience(policy => policy.On<Exception>().Redeliver([TimeSpan.FromMilliseconds(200)]))
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new FlakyMessage("F-1"), CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "The message was not redelivered after failing.");
        Assert.Equal(2, counter.Count);
    }

    [Fact]
    public async Task Redelivery_Should_WaitForTheDelay_When_TheHandlerFails()
    {
        // arrange
        var log = new AttemptLog();
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new InvocationCounter())
            .AddSingleton(recorder)
            .AddSingleton(log)
            .AddMessageBus()
            .AddEventHandler<FailOnceHandler>()
            .AddResilience(policy => policy.On<Exception>().Redeliver([TimeSpan.FromSeconds(2)]))
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new FlakyMessage("F-2"), CancellationToken.None);
        Assert.True(await recorder.WaitAsync(s_timeout), "The message was not redelivered after failing.");

        // assert
        // The configured delay less the 25% jitter, with a margin for clock resolution.
        var attempts = log.Snapshot();
        Assert.Equal(2, attempts.Length);
        Assert.True(
            attempts[1] - attempts[0] >= TimeSpan.FromSeconds(1.4),
            $"The redelivery arrived after {(attempts[1] - attempts[0]).TotalMilliseconds} ms.");
    }

    [Fact]
    public async Task Redelivery_Should_FaultToTheErrorSubject_When_AllAttemptsAreExhausted()
    {
        // arrange
        var counter = new InvocationCounter();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(counter)
            .AddMessageBus()
            .AddEventHandler<AlwaysFailingHandler>()
            .AddResilience(policy => policy.On<Exception>().Redeliver(
                [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200)]))
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        var stream = Assert.Single(bus.Topology.Streams);
        var errorSubject = Assert.Single(
            bus.Topology.Subjects.Select(s => s.Subject),
            s => s.EndsWith("_error", StringComparison.Ordinal));

        // act
        await messageBus.PublishAsync(new UnhandleableMessage("U-2"), CancellationToken.None);

        // assert
        // One delivery and two redeliveries, then the fault.
        Assert.True(
            await HasMessageOnSubjectAsync(stream.Name, errorSubject, s_timeout),
            "The message never reached the error subject.");
        Assert.Equal(3, counter.Count);
    }

    [Fact]
    public async Task Redelivery_Should_ReachOnlyTheFailedHandler_When_AnotherHandlerSharesTheMessage()
    {
        // arrange
        var sibling = new InvocationCounter();
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new InvocationCounter())
            .AddSingleton(recorder)
            .AddSingleton(new AttemptLog())
            .AddKeyedSingleton("sibling", sibling)
            .AddMessageBus()
            .AddEventHandler<FailOnceHandler>()
            .AddEventHandler<SiblingHandler>()
            .AddResilience(policy => policy.On<Exception>().Redeliver([TimeSpan.FromMilliseconds(200)]))
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new FlakyMessage("F-3"), CancellationToken.None);
        Assert.True(await recorder.WaitAsync(s_timeout), "The message was not redelivered after failing.");

        // Long enough for a redelivery fanned out to the sibling to arrive.
        await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None);

        // assert
        Assert.Equal(1, sibling.Count);
    }

    private async Task<bool> HasMessageOnSubjectAsync(string streamName, string subject, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var stream = await fixture.JetStream.GetStreamAsync(
                streamName,
                new StreamInfoRequest { SubjectsFilter = subject },
                CancellationToken.None);

            if (stream.Info.State.Subjects?.ContainsKey(subject) == true)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), CancellationToken.None);
        }

        return false;
    }

    private static async Task<bool> WaitForAtLeastAsync(
        InvocationCounter counter,
        int expected,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (counter.Count >= expected)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);
        }

        return counter.Count >= expected;
    }

    public sealed record UnhandleableMessage(string Id);

    public sealed record FlakyMessage(string Id);

    public sealed class AttemptLog
    {
        private readonly List<DateTimeOffset> _attempts = [];

        public void Add(DateTimeOffset at)
        {
            lock (_attempts)
            {
                _attempts.Add(at);
            }
        }

        public DateTimeOffset[] Snapshot()
        {
            lock (_attempts)
            {
                return [.. _attempts];
            }
        }
    }

    public sealed class FailOnceHandler(InvocationCounter counter, MessageRecorder recorder, AttemptLog log)
        : IEventHandler<FlakyMessage>
    {
        public ValueTask HandleAsync(FlakyMessage message, CancellationToken cancellationToken)
        {
            log.Add(DateTimeOffset.UtcNow);
            counter.Increment();

            if (counter.Count == 1)
            {
                throw new InvalidOperationException("The first attempt fails.");
            }

            recorder.Record(message);

            return ValueTask.CompletedTask;
        }
    }

    public sealed class SiblingHandler([FromKeyedServices("sibling")] InvocationCounter counter)
        : IEventHandler<FlakyMessage>
    {
        public ValueTask HandleAsync(FlakyMessage message, CancellationToken cancellationToken)
        {
            counter.Increment();

            return ValueTask.CompletedTask;
        }
    }

    public sealed class AlwaysFailingHandler(InvocationCounter counter)
        : IEventHandler<UnhandleableMessage>
    {
        public ValueTask HandleAsync(UnhandleableMessage message, CancellationToken cancellationToken)
        {
            counter.Increment();

            throw new InvalidOperationException("This message can never be handled.");
        }
    }
}
