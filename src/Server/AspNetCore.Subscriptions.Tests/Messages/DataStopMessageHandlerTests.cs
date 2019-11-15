using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using HotChocolate.StarWars;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Subscriptions;
using Moq;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class DataStopMessageHandlerTests
    {
        [Fact]
        public void CanHandle_DataStartMessage_True()
        {
            // arrange
            var handler = new DataStopMessageHandler();
            var message = new DataStopMessage("123");

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_KeepAliveMessage_False()
        {
            // arrange
            IQueryExecutor executor = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create()
                .MakeExecutable();

            var handler = new DataStopMessageHandler();
            var message = KeepConnectionAliveMessage.Default;

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.False(result);
        }


        [Fact]
        public async Task Handle_Stop_Subscription()
        {
            // arrange
            var connection = new SocketConnectionMock();

            var services = new ServiceCollection();
            services.AddInMemorySubscriptionProvider();
            services.AddStarWarsRepositories();

            IQueryExecutor executor = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddStarWarsTypes()
                .Create()
                .MakeExecutable();

            DocumentNode query = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEWHOPE) { stars } }");

            IResponseStream stream =
                (IResponseStream)await executor.ExecuteAsync(
                    "subscription { onReview(episode: NEWHOPE) { stars } }");

            var subscription = new Subscription(connection, stream, "123");
            connection.Subscriptions.Register(subscription);

            var handler = new DataStopMessageHandler();
            var message = new DataStopMessage("123");

            // act
            await handler.HandleAsync(
                connection,
                message,
                CancellationToken.None);

            // assert
            Assert.Empty(connection.Subscriptions);
        }
    }
}
