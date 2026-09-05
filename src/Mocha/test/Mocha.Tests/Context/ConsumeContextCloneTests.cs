using Microsoft.Extensions.DependencyInjection;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Tests.Context;

public sealed class ConsumeContextCloneTests
{
    [Fact]
    public void Clone_Should_PreserveCurrentMetadataAndShareReceiveFeatures_When_ReceiveContextIsCloned()
    {
        // arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var envelope = new MessageEnvelope { MessageId = "envelope-message" };
        var context = new ReceiveContext
        {
            Services = null!,
            MessageId = "current-message",
            Envelope = envelope
        };
        context.Headers.Set("x-test", "original");
        context.Features.Set(new CloneFeature());

        // act
        var clone = (ReceiveContext)context.Clone(services);

        // assert
        Assert.Equal(
            ("current-message", "envelope-message", true),
            (clone.MessageId, clone.Envelope?.MessageId, ReferenceEquals(services, clone.Services)));
        Assert.True(clone.Headers.TryGetValue("x-test", out var header) && Equals(header, "original"));
        Assert.NotSame(envelope, clone.Envelope);
        Assert.NotSame(context.Headers, clone.Headers);
        Assert.Same(context.Features.Get<CloneFeature>(), clone.Features.Get<CloneFeature>());
    }

    [Fact]
    public void Clone_Should_PreserveTypedContext_When_TypedContextIsCloned()
    {
        // arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var context = new ConsumeContext<string>(new ReceiveContext());

        // act
        var clone = (ConsumeContext<string>)context.Clone(services);

        // assert
        Assert.Same(services, clone.Services);
    }

    [Fact]
    public void Clone_Should_ReuseBatchAndShareInputFeatures_When_BatchContextIsCloned()
    {
        // arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var firstContext = new ReceiveContext();
        var batch = new MessageBatch<string>(
            [new BufferedEntry<string>(new ConsumeContext<string>(firstContext))],
            BatchCompletionMode.Size);
        var context = new BatchConsumeContext<string>(
            batch,
            null!,
            firstContext,
            "batch",
            null,
            CancellationToken.None);
        context.Features.Set(new CloneFeature());

        // act
        var clone = (BatchConsumeContext<string>)context.Clone(services);

        // assert
        Assert.Same(batch, clone.Message);
        Assert.Same(services, clone.Services);
        Assert.Same(context.Features.Get<CloneFeature>(), clone.Features.Get<CloneFeature>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Clone_Should_KeepAttemptFeaturesOffTheInput_When_CloningAnExecution(bool batch)
    {
        // arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var delivery = new ReceiveContext();
        IConsumeContext input = batch
            ? new BatchConsumeContext<string>(
                new MessageBatch<string>(
                    [new BufferedEntry<string>(new ConsumeContext<string>(delivery))],
                    BatchCompletionMode.Size),
                services,
                delivery,
                "batch",
                null,
                CancellationToken.None)
            : delivery;
        var shared = new CloneFeature();
        input.Features.Set(shared);

        // act
        var first = input.Clone(services);
        first.Features.Get<CloneFeature>()!.Value = 42;
        first.Features.Set(new ExecutionFeature());
        var sibling = input.Clone(services);
        first.Features.Set(new CloneFeature { Value = 99 });

        // assert
        Assert.NotSame(input.Features, first.Features);
        Assert.Equal(42, input.Features.Get<CloneFeature>()!.Value);
        Assert.Null(input.Features.Get<ExecutionFeature>());
        Assert.Same(shared, sibling.Features.Get<CloneFeature>());
        Assert.Null(sibling.Features.Get<ExecutionFeature>());
    }

    [Fact]
    public void Reset_Should_LeaveReceiveFeatureUntouched_When_CloneIsReset()
    {
        // arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var delivery = new ReceiveContext();
        var shared = delivery.Features.GetOrSet<ReceiveConsumerFeature>();
        shared.MessageConsumed = true;
        var clone = (ReceiveContext)delivery.Clone(services);

        // act
        clone.Reset();

        // assert
        Assert.True(shared.MessageConsumed);
        Assert.True(clone.Features.IsEmpty);
    }

    [Fact]
    public void Clone_Should_ReuseResetContext_When_ReturnedToPool()
    {
        // arrange
        using var services = new ServiceCollection().BuildServiceProvider();
        var pool = new ReceiveContextPool(1);
        var firstDelivery = new ReceiveContext();
        var shared = firstDelivery.Features.GetOrSet<ReceiveConsumerFeature>();
        shared.MessageConsumed = true;
        var first = firstDelivery.CopyTo(pool.Get(), services);
        first.Headers.Set("old", "value");

        // act
        pool.Return(first);
        var secondDelivery = new ReceiveContext { MessageId = "second" };
        var second = secondDelivery.CopyTo(pool.Get(), services);

        // assert
        Assert.Same(first, second);
        Assert.Equal("second", second.MessageId);
        Assert.True(second.Features.IsEmpty);
        Assert.Empty(second.Headers);
        Assert.True(shared.MessageConsumed);
        pool.Return(second);
    }

    private sealed class CloneFeature
    {
        public int Value { get; set; }
    }

    private sealed class ExecutionFeature;
}
