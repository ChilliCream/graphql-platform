using Microsoft.Extensions.DependencyInjection;
using Mocha.SchedulingOff.Contracts;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class DeclaredStreamSchedulingTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task ProvisionAsync_Should_KeepSchedulingSettings_When_SchedulingIsTurnedOffOnADeclaredStream()
    {
        // arrange
        // The server refuses to turn message TTL or schedules off once a stream has them, so a
        // service that drops scheduling must still start against the stream it provisioned before.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = await fixture.CreateScopeAsync();
        await BootAsync(enableScheduling: true);

        // act
        await BootAsync(enableScheduling: false);

        // assert
        var stream = await fixture.JetStream.GetStreamAsync(
            "SCHEDULING_OFF_SERVICE",
            cancellationToken: cancellationToken);
        Assert.True(stream.Info.Config.AllowMsgTTL);
        Assert.True(stream.Info.Config.AllowMsgSchedules);
    }

    private async Task BootAsync(bool enableScheduling)
    {
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(new MessageRecorder())
            .AddMessageBus()
            .AddEventHandler<SchedulingOffHandler>()
            .AddNats(nats =>
            {
                nats.StreamName("scheduling-off-service").EnableScheduling(enableScheduling);
                nats.DeclareStream("SCHEDULING_OFF_SERVICE");
            })
            .BuildTestBusAsync();
    }

    public sealed class SchedulingOffHandler(MessageRecorder recorder) : IEventHandler<SchedulingOffRequested>
    {
        public ValueTask HandleAsync(SchedulingOffRequested message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
