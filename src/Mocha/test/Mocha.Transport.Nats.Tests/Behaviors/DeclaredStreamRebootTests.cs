using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Reboot.Contracts;
using Mocha.Transport.Nats.Tests.Helpers;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class DeclaredStreamRebootTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task ProvisionAsync_Should_KeepDerivedSubjects_When_ADeclaredStreamIsProvisionedTwice()
    {
        // arrange
        // The declaration covers the contracts namespace; the transport adds the fault subjects.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = await fixture.CreateScopeAsync();

        // act
        var first = await BootAndReadSubjectsAsync(cancellationToken);
        var second = await BootAndReadSubjectsAsync(cancellationToken);
        var third = await BootAndReadSubjectsAsync(cancellationToken);

        // assert
        Assert.Equal(
            [
                "mocha.reboot.contracts.>",
                "mocha.transport.nats.tests.reboot_error",
                "mocha.transport.nats.tests.reboot_skipped"
            ],
            first);

        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    private async Task<List<string>> BootAndReadSubjectsAsync(CancellationToken cancellationToken)
    {
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<RebootHandler>()
            .AddNats(nats =>
            {
                nats.StreamName("reboot-service");
                nats.DeclareStream("REBOOT_SERVICE")
                    .Subject("mocha.reboot.contracts.>")
                    .Storage(StreamConfigStorage.Memory);
            })
            .BuildTestBusAsync();

        var stream = await fixture.JetStream.GetStreamAsync(
            "REBOOT_SERVICE",
            cancellationToken: cancellationToken);

        return [.. (stream.Info.Config.Subjects ?? []).Order(StringComparer.Ordinal)];
    }

    public sealed class RebootHandler(MessageRecorder recorder) : IEventHandler<RebootRequested>
    {
        public ValueTask HandleAsync(RebootRequested message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
