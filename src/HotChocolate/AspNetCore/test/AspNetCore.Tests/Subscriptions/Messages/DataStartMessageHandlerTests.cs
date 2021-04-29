using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class DataStartMessageHandlerTests
    {
        [Fact]
        public void CanHandle_DataStartMessage_True()
        {
            // arrange
            var interceptor = new DefaultSocketSessionInterceptor();
            IRequestExecutor executor = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create()
                .MakeExecutable();
            DocumentNode query = Utf8GraphQLParser.Parse("{ hero { name } }");
            var handler = new DataStartMessageHandler(
                executor,
                interceptor,
                new NoopDiagnosticEvents());

            var message = new DataStartMessage(
                "123",
                new GraphQLRequest(query));

            // act
            var result = handler.CanHandle(message);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanHandle_KeepAliveMessage_False()
        {
            // arrange
            var interceptor = new DefaultSocketSessionInterceptor();
            IRequestExecutor executor = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create()
                .MakeExecutable();
            var handler = new DataStartMessageHandler(
                executor,
                interceptor,
                new NoopDiagnosticEvents());
            KeepConnectionAliveMessage message = KeepConnectionAliveMessage.Default;

            // act
            var result = handler.CanHandle(message);

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task Handle_Query_DataReceived_And_Completed()
        {
            // arrange
            IServiceProvider services = new ServiceCollection()
                .AddGraphQL()
                .AddStarWarsTypes()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor = await services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

            var interceptor = new SocketSessionInterceptorMock();
            var connection = new SocketConnectionMock { RequestServices = services };
            DocumentNode query = Utf8GraphQLParser.Parse("{ hero { name } }");
            var handler = new DataStartMessageHandler(
                executor,
                interceptor,
                new NoopDiagnosticEvents());
            var message = new DataStartMessage("123", new GraphQLRequest(query));

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
            IServiceProvider services = new ServiceCollection()
                .AddGraphQL()
                .AddStarWarsTypes()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor = await services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

            var interceptor = new SocketSessionInterceptorMock();
            var connection = new SocketConnectionMock { RequestServices = services };
            DocumentNode query = Utf8GraphQLParser.Parse("{ hero { name } }");
            var handler = new DataStartMessageHandler(
                executor,
                interceptor,
                new NoopDiagnosticEvents());
            var message = new DataStartMessage("123", new GraphQLRequest(query));

            var result = (IReadOnlyQueryResult)await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetServices(services)
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
            IServiceProvider services = new ServiceCollection()
                .AddGraphQL()
                .AddStarWarsTypes()
                .AddStarWarsRepositories()
                .AddInMemorySubscriptions()
                .Services
                .BuildServiceProvider();

            IRequestExecutor executor = await services
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();

            var interceptor = new SocketSessionInterceptorMock();
            var connection = new SocketConnectionMock { RequestServices = services };
            DocumentNode query = Utf8GraphQLParser.Parse(
                "subscription { onReview(episode: NEW_HOPE) { stars } }");
            var handler = new DataStartMessageHandler(
                executor,
                interceptor,
                new NoopDiagnosticEvents());
            var message = new DataStartMessage("123", new GraphQLRequest(query));

            // act
            await handler.HandleAsync(
                connection,
                message,
                CancellationToken.None);

            // assert
            Assert.Empty(connection.SentMessages);
            Assert.NotEmpty(connection.Subscriptions);

            var stream =
                (IResponseStream)await executor.ExecuteAsync(
                    "subscription { onReview(episode: NEW_HOPE) { stars } }");

            await executor.ExecuteAsync(@"
                mutation {
                    createReview(episode:NEW_HOPE review:
                        {
                            commentary: ""foo""
                            stars: 5
                        }) {
                        stars
                    }
                }");

            using var cts = new CancellationTokenSource(15000);
            ConfiguredCancelableAsyncEnumerable<IQueryResult>.Enumerator enumerator =
                stream.ReadResultsAsync().WithCancellation(cts.Token).GetAsyncEnumerator();
            Assert.True(await enumerator.MoveNextAsync());

            await Task.Delay(2000, cts.Token);

            Assert.Collection(connection.SentMessages,
                t =>
                {
                    Assert.True(t.SequenceEqual(
                        new DataResultMessage(message.Id, enumerator.Current).Serialize()));
                });
        }
    }
}
