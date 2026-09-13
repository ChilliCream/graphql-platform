using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class RedeliveryTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task Handler_Should_StopBeingRedelivered_When_TheMessageHasBeenDeadLettered()
    {
        // arrange
        // A failed pipeline negatively acknowledges so the message is released rather than waiting out
        // AckWait. Once it has been dead-lettered it has to be settled instead, because the broker's
        // own delivery limit is unlimited by default and an unbounded redelivery loop would spin.
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

        // Long enough for another redelivery to arrive if the message was left unsettled. AckWait
        // defaults to 30 seconds, and a negative acknowledgement releases immediately.
        await Task.Delay(TimeSpan.FromSeconds(10), CancellationToken.None);

        // assert
        Assert.Equal(2, counter.Count);
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
