using System;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionManagerTests
    {
        [Fact]
        public void Create_Instance_Connection_Is_Null()
        {
            // arrange
            // act
            Action action = () => new SubscriptionManager(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Register_Subscription()
        {
            // arrange
            var connection = new SocketConnectionMock();
            var subscriptions = new SubscriptionManager(connection);
            var subscription = new SubscriptionMock();

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
            var connection = new SocketConnectionMock();
            var subscriptions = new SubscriptionManager(connection);
            var subscription = new SubscriptionMock();

            // act
            Action action = () => subscriptions.Register(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Register_Subscription_ManagerAlreadyDisposed()
        {
            // arrange
            var connection = new SocketConnectionMock();
            var subscriptions = new SubscriptionManager(connection);
            var subscription = new SubscriptionMock { Id = "abc" };

            subscriptions.Register(subscription);
            subscriptions.Dispose();

            // act
            Action action = () =>
                subscriptions.Register(new SubscriptionMock { Id = "def" });

            // assert
            Assert.Throws<ObjectDisposedException>(action);
        }

        [Fact]
        public void Unregister_Subscription()
        {
            // arrange
            var connection = new SocketConnectionMock();
            var subscriptions = new SubscriptionManager(connection);
            var subscription = new SubscriptionMock();
            subscriptions.Register(subscription);
            Assert.Collection(subscriptions,
                t => Assert.Equal(subscription, t));

            // act
            subscriptions.Unregister(subscription.Id);

            // assert
            Assert.Empty(subscriptions);
        }

        [Fact]
        public void Unregister_Subscription_SubscriptionIdIsNull()
        {
            // arrange
            var connection = new SocketConnectionMock();
            var subscriptions = new SubscriptionManager(connection);
            var subscription = new SubscriptionMock();
            subscriptions.Register(subscription);
            Assert.Collection(subscriptions,
                t => Assert.Equal(subscription, t));

            // act
            Action action = () => subscriptions.Unregister(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Unregister_Subscription_ManagerAlreadyDisposed()
        {
            // arrange
            var connection = new SocketConnectionMock();
            var subscriptions = new SubscriptionManager(connection);
            var subscription = new SubscriptionMock { Id = "abc" };

            subscriptions.Register(subscription);
            subscriptions.Dispose();

            // act
            Action action = () => subscriptions.Unregister("abc");

            // assert
            Assert.Throws<ObjectDisposedException>(action);
        }

        [Fact]
        public void Dispose_Subscriptions()
        {
            // arrange
            var connection = new SocketConnectionMock();
            var subscriptions = new SubscriptionManager(connection);
            var subscription_a = new SubscriptionMock();
            var subscription_b = new SubscriptionMock();

            subscriptions.Register(subscription_a);
            subscriptions.Register(subscription_b);

            // act
            subscriptions.Dispose();

            // assert
            Assert.Empty(subscriptions);
            Assert.True(subscription_a.IsDisposed);
            Assert.True(subscription_a.IsDisposed);
        }

        [Fact]
        public void Complete_Subscription()
        {
            // arrange
            var connection = new SocketConnectionMock();
            var subscriptions = new SubscriptionManager(connection);
            var subscription = new SubscriptionMock();
            subscriptions.Register(subscription);
            Assert.Collection(subscriptions,
                t => Assert.Equal(subscription, t));

            // act
            subscription.Complete();

            // assert
            Assert.Empty(subscriptions);
        }
    }
}
