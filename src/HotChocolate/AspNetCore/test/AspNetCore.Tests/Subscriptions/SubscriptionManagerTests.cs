using System;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions;

public class SubscriptionManagerTests
{
    [Fact]
    public void Register_Subscription()
    {
        // arrange
        var subscriptions = new SubscriptionManager();
        var subscription = new SubscriptionSessionMock();

        // act
        subscriptions.Register(subscription);

        // assert
        Assert.Collection(subscriptions,
            t => Assert.Equal(subscription, t));
    }

    [Fact]
    public void Register_Subscription_SubscriptionIsNull()
    {
        // arrange
        var subscriptions = new SubscriptionManager();

        // act
        void Action() => subscriptions.Register(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Register_Subscription_ManagerAlreadyDisposed()
    {
        // arrange
        var subscriptions = new SubscriptionManager();
        var subscription = new SubscriptionSessionMock { Id = "abc" };

        subscriptions.Register(subscription);
        subscriptions.Dispose();

        // act
        void Action() => subscriptions.Register(new SubscriptionSessionMock { Id = "def" });

        // assert
        Assert.Throws<ObjectDisposedException>(Action);
    }

    [Fact]
    public void Unregister_Subscription()
    {
        // arrange
        var subscriptions = new SubscriptionManager();
        var subscription = new SubscriptionSessionMock();
        subscriptions.Register(subscription);
        Assert.Collection(subscriptions, t => Assert.Equal(subscription, t));

        // act
        subscriptions.Unregister(subscription.Id);

        // assert
        Assert.Empty(subscriptions);
    }

    [Fact]
    public void Unregister_Subscription_SubscriptionIdIsNull()
    {
        // arrange
        var subscriptions = new SubscriptionManager();
        var subscription = new SubscriptionSessionMock();
        subscriptions.Register(subscription);
        Assert.Collection(subscriptions, t => Assert.Equal(subscription, t));

        // act
        void Action() => subscriptions.Unregister(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Unregister_Subscription_ManagerAlreadyDisposed()
    {
        // arrange
        var subscriptions = new SubscriptionManager();
        var subscription = new SubscriptionSessionMock { Id = "abc" };

        subscriptions.Register(subscription);
        subscriptions.Dispose();

        // act
        void Action() => subscriptions.Unregister("abc");

        // assert
        Assert.Throws<ObjectDisposedException>(Action);
    }

    [Fact]
    public void Dispose_Subscriptions()
    {
        // arrange
        var subscriptions = new SubscriptionManager();
        var subscriptionA = new SubscriptionSessionMock();
        var subscriptionB = new SubscriptionSessionMock();

        subscriptions.Register(subscriptionA);
        subscriptions.Register(subscriptionB);

        // act
        subscriptions.Dispose();

        // assert
        Assert.Empty(subscriptions);
        Assert.True(subscriptionA.IsDisposed);
        Assert.True(subscriptionA.IsDisposed);
    }

    [Fact]
    public void Complete_Subscription()
    {
        // arrange
        var subscriptions = new SubscriptionManager();
        var subscription = new SubscriptionSessionMock();
        subscriptions.Register(subscription);
        Assert.Collection(subscriptions, t => Assert.Equal(subscription, t));

        // act
        subscription.Complete();

        // assert
        Assert.Empty(subscriptions);
    }
}
