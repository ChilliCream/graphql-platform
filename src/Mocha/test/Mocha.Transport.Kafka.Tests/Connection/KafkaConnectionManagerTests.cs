using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.Kafka.Connection;

namespace Mocha.Transport.Kafka.Tests.Connection;

public class KafkaConnectionManagerTests
{
    [Fact]
    public void Producer_Should_Throw_When_NotCreated()
    {
        // arrange
        var manager = CreateManager();

        // act & assert
        Assert.Throws<InvalidOperationException>(() => _ = manager.Producer);
    }

    [Fact]
    public void EnsureProducerCreated_Should_CreateProducer_When_Called()
    {
        // arrange
        using var manager = new DisposableManager(CreateManager());

        // act
        manager.Value.EnsureProducerCreated();

        // assert - should not throw
        Assert.NotNull(manager.Value.Producer);
    }

    [Fact]
    public void EnsureProducerCreated_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        using var manager = new DisposableManager(CreateManager());

        // act
        manager.Value.EnsureProducerCreated();
        var first = manager.Value.Producer;
        manager.Value.EnsureProducerCreated();
        var second = manager.Value.Producer;

        // assert - same producer instance
        Assert.Same(first, second);
    }

    [Fact]
    public async Task TrackInflight_Should_CancelTcs_When_Disposed()
    {
        // arrange
        var manager = CreateManager();
        manager.EnsureProducerCreated();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.TrackInflight(tcs);

        // act
        await manager.DisposeAsync();

        // assert
        Assert.True(tcs.Task.IsCanceled);
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        var manager = CreateManager();
        manager.EnsureProducerCreated();

        // act
        await manager.DisposeAsync();
        var exception = await Record.ExceptionAsync(() => manager.DisposeAsync().AsTask());

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_Should_NotThrow_When_ProducerNotCreated()
    {
        // arrange
        var manager = CreateManager();

        // act & assert - no exception
        var exception = await Record.ExceptionAsync(() => manager.DisposeAsync().AsTask());
        Assert.Null(exception);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static KafkaConnectionManager CreateManager()
    {
        var logger = NullLogger<KafkaConnectionManager>.Instance;
        return new KafkaConnectionManager(logger, "localhost:9092", null, null);
    }

    /// <summary>
    /// Wrapper that calls DisposeAsync synchronously on Dispose for test cleanup.
    /// </summary>
    private sealed class DisposableManager(KafkaConnectionManager value) : IDisposable
    {
        public KafkaConnectionManager Value => value;

        public void Dispose()
        {
            value.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
