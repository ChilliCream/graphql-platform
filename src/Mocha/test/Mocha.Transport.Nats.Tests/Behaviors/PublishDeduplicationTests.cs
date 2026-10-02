using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class PublishDeduplicationTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PublishAsync_Should_DeliverBoth_When_PublishDeduplicationIsDefault()
    {
        // arrange
        // The default has to leave the header off. With it on, the stream drops the second publish
        // and acknowledges it as stored, so a deliberate republish disappears without an error.
        var recorder = new MessageRecorder();

        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await BuildAsync(scope, recorder, deduplicate: false);

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new DedupEvent { Payload = "first" }, CancellationToken.None);
        await messageBus.PublishAsync(new DedupEvent { Payload = "second" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(s_timeout, expectedCount: 2),
            "Handler should receive both publishes when the deduplication header is not sent");

        var payloads = recorder.Messages.Cast<DedupEvent>().Select(e => e.Payload).OrderBy(p => p).ToList();

        Assert.Equal(["first", "second"], payloads);
    }

    [Fact]
    public async Task PublishAsync_Should_DropRepeat_When_PublishDeduplicationEnabled()
    {
        // arrange
        var recorder = new MessageRecorder();

        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await BuildAsync(scope, recorder, deduplicate: true);

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new DedupEvent { Payload = "first" }, CancellationToken.None);
        await messageBus.PublishAsync(new DedupEvent { Payload = "second" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the first publish");

        Assert.False(
            await recorder.WaitAsync(TimeSpan.FromSeconds(3), expectedCount: 2),
            "The stream should have discarded the repeated message identifier");

        var message = Assert.Single(recorder.Messages);

        Assert.Equal("first", Assert.IsType<DedupEvent>(message).Payload);
    }

    private Task<TestBus> BuildAsync(
        JetStreamScope scope,
        MessageRecorder recorder,
        bool deduplicate)
    {
        var fixedMessageId = "dedup-" + scope.StreamName;

        var services = new ServiceCollection();
        services.AddSingleton(fixture.Connection);
        services.AddSingleton(recorder);

        var builder = services
            .AddMessageBus()
            .AddEventHandler<DedupEventHandler>();

        // Both publishes carry one identifier, which is what the stream deduplicates on.
        builder.ConfigureMessageBus(h =>
            h.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) =>
                        ctx =>
                        {
                            ctx.MessageId = fixedMessageId;
                            return next(ctx);
                        },
                    "ForceMessageId"),
                before: "Instrumentation"));

        return builder
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);
                nats.EnablePublishDeduplication(deduplicate);
            })
            .BuildTestBusAsync();
    }

    public sealed class DedupEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class DedupEventHandler(MessageRecorder recorder) : IEventHandler<DedupEvent>
    {
        public ValueTask HandleAsync(DedupEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }
}
