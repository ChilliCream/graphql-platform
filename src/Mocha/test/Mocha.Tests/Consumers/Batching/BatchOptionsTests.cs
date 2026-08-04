using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Consumers.Batching;

public sealed class BatchOptionsTests
{
    [Fact]
    public void Defaults_Should_HaveExpectedValues_When_Created()
    {
        // arrange & act
        var options = new BatchOptions();

        // assert
        Assert.Equal(100, options.MaxBatchSize);
        Assert.Equal(TimeSpan.FromSeconds(1), options.BatchTimeout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AddBatchHandler_Should_ThrowArgumentOutOfRange_When_MaxBatchSizeInvalid(int value)
    {
        // arrange & act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateRuntime(b => b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = value))
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddBatchHandler_Should_ThrowArgumentOutOfRange_When_BatchTimeoutInvalid(int ms)
    {
        // arrange & act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateRuntime(b =>
                b.AddBatchHandler<TestBatchHandler>(opts => opts.BatchTimeout = TimeSpan.FromMilliseconds(ms))
            )
        );
    }

    [Fact]
    public void AddBatchHandler_Should_Succeed_When_MaxBatchSizeIsOne()
    {
        // arrange & act - edge case: batch size of 1 is valid
        var runtime = CreateRuntime(b => b.AddBatchHandler<TestBatchHandler>(opts => opts.MaxBatchSize = 1));

        // assert
        Assert.NotNull(runtime);
    }

    // --- Helpers ---

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    // --- Test types ---

    public sealed class TestEvent
    {
        public required string Id { get; init; }
    }

    public sealed class TestBatchHandler : IBatchEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestEvent> batch, CancellationToken cancellationToken) => default;
    }
}
