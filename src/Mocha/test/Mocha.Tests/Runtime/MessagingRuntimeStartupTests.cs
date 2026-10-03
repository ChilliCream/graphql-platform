using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public sealed class MessagingRuntimeStartupTests
{
    [Fact]
    public async Task StartAsync_Should_RollBackEndpoints_When_EndpointStartupFails()
    {
        // arrange
        var services = new ServiceCollection();
        var resolutions = 0;
        var failure = new InvalidOperationException("Endpoint startup failed.");
        services.AddMessageBus().AddEventHandler<TestHandler>().AddInMemory()
            .ConfigureMessageBus(b => b.ConfigureServices(s =>
                s.AddTransient<ILogger<ReceiveEndpoint>>(_ =>
                {
                    if (Interlocked.Increment(ref resolutions) == 2)
                    {
                        throw failure;
                    }

                    return NullLogger<ReceiveEndpoint>.Instance;
                })));
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        var exception = await Record.ExceptionAsync(() => runtime.StartAsync(CancellationToken.None).AsTask());
        var endpointsStartedAfterFailure = runtime.Transports.SelectMany(t => t.ReceiveEndpoints).Count(e => e.IsStarted);

        // assert
        new
        {
            OriginalFailure = ReferenceEquals(failure, exception),
            EndpointsStartedAfterFailure = endpointsStartedAfterFailure,
            RuntimeStarted = runtime.IsStarted
        }.MatchInlineSnapshot(
            """
            {
              "OriginalFailure": true,
              "EndpointsStartedAfterFailure": 0,
              "RuntimeStarted": false
            }
            """);
    }

    public sealed class TestMessage;

    public sealed class TestHandler : IEventHandler<TestMessage>
    {
        public ValueTask HandleAsync(TestMessage message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
