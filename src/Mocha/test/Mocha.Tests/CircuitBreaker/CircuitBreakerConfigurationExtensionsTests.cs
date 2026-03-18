using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class CircuitBreakerConfigurationExtensionsTests
{
    [Fact]
    public void AddCircuitBreaker_Should_ConfigureSuccessfully_When_CalledOnHostBuilder()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();

        builder.AddCircuitBreaker(o =>
        {
            o.FailureRatio = 0.5;
            o.MinimumThroughput = 3;
            o.BreakDuration = TimeSpan.FromSeconds(5);
        });

        builder.AddEventHandler<AlwaysSucceedHandler>();
        builder.Services.AddSingleton(new MessageRecorder());
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // assert
        var result = runtime.Features.Get<CircuitBreakerFeature>();
        Assert.NotNull(result);
        Assert.Equal(0.5, result.FailureRatio);
        Assert.Equal(3, result.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(5), result.BreakDuration);
    }

    [Fact]
    public void AddCircuitBreaker_Should_Override_OnTransport()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();

        builder.AddCircuitBreaker(o =>
        {
            o.FailureRatio = 0.5;
            o.MinimumThroughput = 3;
            o.BreakDuration = TimeSpan.FromSeconds(5);
        });

        builder.AddEventHandler<AlwaysSucceedHandler>();
        builder.Services.AddSingleton(new MessageRecorder());
        builder.AddInMemory(descriptor => descriptor.AddCircuitBreaker(o => o.FailureRatio = 0.1));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // assert
        var result = runtime.Transports[0].Features.Get<CircuitBreakerFeature>();
        Assert.NotNull(result);
        Assert.Equal(0.1, result.FailureRatio);
    }

    [Fact]
    public void AddCircuitBreaker_Should_Override_OnEndpoint()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();

        builder.AddEventHandler<AlwaysSucceedHandler>();
        builder.Services.AddSingleton(new MessageRecorder());

        // act
        builder.AddInMemory(descriptor =>
            descriptor.Endpoint("endpoint1").AddCircuitBreaker(o => o.FailureRatio = 0.2));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var result = runtime.Transports[0].ReceiveEndpoints.First().Features.Get<CircuitBreakerFeature>();
        Assert.NotNull(result);
        Assert.Equal(0.2, result.FailureRatio);
    }

    public sealed class AlwaysSucceedHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken ct)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class TestEvent
    {
        public required string Data { get; init; }
    }
}
