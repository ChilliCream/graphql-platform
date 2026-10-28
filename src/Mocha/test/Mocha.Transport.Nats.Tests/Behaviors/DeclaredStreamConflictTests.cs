using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Conflict.Contracts;
using Mocha.Narrow.Contracts;
using Mocha.Transport.Nats.Tests.Fixtures;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class DeclaredStreamConflictTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task StartAsync_Should_Throw_When_ADeclaredStreamsSubjectsAreOwnedElsewhere()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        await fixture.JetStream.CreateOrUpdateStreamAsync(
            new StreamConfig
            {
                Name = "CONFLICT_OWNER",
                Subjects = ["mocha.conflict.contracts.>"],
                Storage = StreamConfigStorage.Memory
            },
            cancellationToken);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<ConflictingHandler>()
            .AddNats(nats =>
            {
                nats.StreamName("conflict-declarer");

                nats.Endpoint("conflict-endpoint")
                    .Handler<ConflictingHandler>()
                    .Subject("mocha.conflict.contracts.thing-happened");

                nats.DeclareStream("CONFLICT_DECLARED")
                    .Subject("mocha.conflict.contracts.>")
                    .Storage(StreamConfigStorage.Memory);
            });

        using var host = builder.Build();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await host.StartAsync(cancellationToken));

        // assert
        Assert.Equal(
            "Stream 'CONFLICT_DECLARED' cannot be provisioned because its subjects overlap a stream "
            + "that already exists: 'mocha.conflict.contracts.>' is already captured by stream "
            + "'CONFLICT_OWNER'. JetStream requires every stream's subjects to be disjoint. Either "
            + "remove the overlapping subject from this declaration, delete the stream that owns it, "
            + "or drop the declaration and let the transport bind to the existing stream.",
            exception.Message);
    }

    [Fact]
    public async Task ProvisionAsync_Should_DropRedundantSubjects_When_AWildcardCoversAnExistingOne()
    {
        // arrange
        // An older stream of the same name holds a subject the new wildcard covers.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        await fixture.JetStream.CreateOrUpdateStreamAsync(
            new StreamConfig
            {
                Name = "NARROW_SERVICE",
                Subjects = ["mocha.narrow.contracts.i-thing"],
                Storage = StreamConfigStorage.File
            },
            cancellationToken);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<NarrowHandler>()
            .AddNats(nats => nats.StreamName("narrow-service"));

        using var host = builder.Build();

        // act
        await host.StartAsync(cancellationToken);

        try
        {
            // assert
            var stream = await fixture.JetStream.GetStreamAsync(
                "NARROW_SERVICE",
                cancellationToken: cancellationToken);

            // The pre-existing narrow subject is kept, since nothing here covers it.
            Assert.Contains("mocha.narrow.contracts.i-thing", stream.Info.Config.Subjects ?? []);
            Assert.Equal(StreamConfigStorage.File, stream.Info.Config.Storage);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    public sealed class ConflictingHandler : IEventHandler<ThingHappened>
    {
        public ValueTask HandleAsync(ThingHappened message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public sealed class NarrowHandler(MessageRecorder recorder) : IEventHandler<NarrowThing>
    {
        public ValueTask HandleAsync(NarrowThing message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
