using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SubscriptionStartHandlerTests
    {
        [Fact]
        public void CanHandle_SubscriptionStart_True()
        {
            // arrange
            var handler = new SubscriptionStartHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Subscription.Start
            };

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_SubscriptionStop_False()
        {
            // arrange
            var handler = new SubscriptionStartHandler();
            var message = new GenericOperationMessage
            {
                Type = MessageTypes.Subscription.Stop
            };

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.False(result);
        }
    }
}
