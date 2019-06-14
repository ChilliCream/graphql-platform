using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionStopHandlerTests
    {
        [Fact]
        public void CanHandle_SubscriptionStop_True()
        {
            // arrange
            var handler = new SubscriptionStopHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Subscription.Stop
            };

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_SubscriptionStart_False()
        {
            // arrange
            var handler = new SubscriptionStopHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Subscription.Start
            };

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.False(result);
        }
    }
}
