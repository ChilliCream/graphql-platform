namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQCompletedDeliveriesTests
{
    [Fact]
    public void Add_Should_ForgetOldestMessage_When_CapacityExceeded()
    {
        // arrange
        var completed = new RabbitMQCompletedDeliveries(capacity: 2);

        // act
        completed.Add("a");
        completed.Add("b");
        completed.Add("c");

        // assert
        Assert.False(completed.TryRemove("a"));
        Assert.True(completed.TryRemove("b"));
        Assert.True(completed.TryRemove("c"));
    }

    [Fact]
    public void TryRemove_Should_ReturnFalse_When_MessageAlreadyRemoved()
    {
        // arrange
        var completed = new RabbitMQCompletedDeliveries(capacity: 2);
        completed.Add("a");

        // act
        var first = completed.TryRemove("a");
        var second = completed.TryRemove("a");

        // assert
        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void Add_Should_KeepCapacity_When_MessageAddedAgainAfterRemoval()
    {
        // arrange
        var completed = new RabbitMQCompletedDeliveries(capacity: 2);
        completed.Add("a");
        completed.TryRemove("a");

        // act
        completed.Add("a");
        completed.Add("b");
        completed.Add("c");

        // assert
        Assert.False(completed.TryRemove("a"));
        Assert.True(completed.TryRemove("b"));
        Assert.True(completed.TryRemove("c"));
    }
}
