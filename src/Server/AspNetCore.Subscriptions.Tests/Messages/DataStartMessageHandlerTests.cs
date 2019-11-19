using System.Linq;
using System.Collections.Generic;
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
    public class DataStartMessageHandlerTests
    {
        [Fact]
        public void CanHandle_DataStartMessage_True()
        {
            // arrange
            IQueryExecutor executor = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create()
                .MakeExecutable();

            DocumentNode query = Utf8GraphQLParser.Parse("{ hero { name } }");

            var handler = new DataStartMessageHandler(executor, null);

            var message = new DataStartMessage(
                "123",
                new GraphQLRequest(query));

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

            var handler = new DataStartMessageHandler(executor, null);
            var message = KeepConnectionAliveMessage.Default;

            // act
            bool result = handler.CanHandle(message);

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_Query_DataReceived_And_Completed()
        {
            // arrange
            var connection = new SocketConnectionMock();

            IQueryExecutor executor = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create()
                .MakeExecutable();

            DocumentNode query = Utf8GraphQLParser.Parse("{ hero { name } }");

            var handler = new DataStartMessageHandler(executor, null);

            var message = new DataStartMessage(
                "123",
                new GraphQLRequest(query));

            var result = (IReadOnlyQueryResult)await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create());

            // act
            await handler.HandleAsync(
                connection,
                message,
                CancellationToken.None);

            // assert
            Assert.Collection(connection.SentMessages,
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        new DataResultMessage(message.Id, result).Serialize()));
                },
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        new DataCompleteMessage(message.Id).Serialize()));
                });
        }

        [Fact]
        public async Task Handle_Query_With_Inter_DataReceived_And_Completed()
        {
            // arrange
            var interceptor = new Mock<ISocketQueryRequestInterceptor>();
            interceptor.Setup(t => t.OnCreateAsync(
                It.IsAny<ISocketConnection>(),
                It.IsAny<IQueryRequestBuilder>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var connection = new SocketConnectionMock();

            IQueryExecutor executor = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create()
                .MakeExecutable();

            DocumentNode query = Utf8GraphQLParser.Parse("{ hero { name } }");

            var handler = new DataStartMessageHandler(
                executor,
                new List<ISocketQueryRequestInterceptor> { interceptor.Object } );

            var message = new DataStartMessage(
                "123",
                new GraphQLRequest(query));

            var result = (IReadOnlyQueryResult)await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create());

            // act
            await handler.HandleAsync(
                connection,
                message,
                CancellationToken.None);

            // assert
            Assert.Collection(connection.SentMessages,
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        new DataResultMessage(message.Id, result).Serialize()));
                },
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        new DataCompleteMessage(message.Id).Serialize()));
                });
        }

        [Fact]
        public async Task Handle_Subscription_DataReceived_And_Completed()
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

            var handler = new DataStartMessageHandler(executor, null);

            var message = new DataStartMessage(
                "123",
                new GraphQLRequest(query));

            // act
            await handler.HandleAsync(
                connection,
                message,
                CancellationToken.None);

            // assert
            Assert.Empty(connection.SentMessages);
            Assert.NotEmpty(connection.Subscriptions);

            IResponseStream stream =
                (IResponseStream)await executor.ExecuteAsync(
                    "subscription { onReview(episode: NEWHOPE) { stars } }");

            await executor.ExecuteAsync(@"
                mutation {
                    createReview(episode:NEWHOPE review:
                        {
                            commentary: ""foo""
                            stars: 5
                        }) {
                        stars
                    }
                }");

            IReadOnlyQueryResult result = await stream.ReadAsync();

            await Task.Delay(2000);

            Assert.Collection(connection.SentMessages,
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        new DataResultMessage(message.Id, result).Serialize()));
                });
        }
    }
}
