using System.Buffers;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.AspNetCore.Subscriptions;

public class OperationManagerTests
{
    [Fact]
    public async Task Enqueue_On_Disposed_Manager()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .AddInMemorySubscriptions()
                .AddSocketSessionInterceptor<TestSocketSessionInterceptor>()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");

        var operations = new OperationManager(
            socketSession,
            new ExecutorSession(executor));
        socketSession.Operations = operations;
        operations.Dispose();

        // act
        var query = Utf8GraphQLParser.Parse(
            "subscription { onReview(episode: NEW_HOPE) { stars } }");
        var request = new GraphQLRequest(query);
        void Fail() => operations.Enqueue("abc", request);

        // assert
        Assert.Throws<ObjectDisposedException>(Fail);
    }

    [Fact]
    public async Task Enqueue_Request()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .AddInMemorySubscriptions()
                .AddSocketSessionInterceptor<TestSocketSessionInterceptor>()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");

        using var operations = new OperationManager(
            socketSession,
            new ExecutorSession(executor));
        socketSession.Operations = operations;

        var query = Utf8GraphQLParser.Parse(
            "subscription { onReview(episode: NEW_HOPE) { stars } }");
        var request = new GraphQLRequest(query);

        // act
        var success = operations.Enqueue("abc", request);
        var registered = operations.ToArray();

        // assert
        Assert.True(success);
        Assert.Collection(registered, t => Assert.Equal("abc", t.Id));
    }

    [Fact]
    public async Task Enqueue_Request_With_Non_Unique_Id()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .AddInMemorySubscriptions()
                .AddSocketSessionInterceptor<TestSocketSessionInterceptor>()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");

        using var operations = new OperationManager(
            socketSession,
            new ExecutorSession(executor));
        socketSession.Operations = operations;

        var query = Utf8GraphQLParser.Parse(
            "subscription { onReview(episode: NEW_HOPE) { stars } }");
        var request = new GraphQLRequest(query);
        var success1 = operations.Enqueue("abc", request);
        var registered1 = operations.ToArray();

        // act
        var success2 = operations.Enqueue("abc", request);
        var registered2 = operations.ToArray();

        // assert
        Assert.True(success1);
        Assert.Collection(registered1, t => Assert.Equal("abc", t.Id));
        Assert.False(success2);
        Assert.Collection(registered2, t => Assert.Equal("abc", t.Id));
    }

    [Fact]
    public async Task Complete_Request()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .AddInMemorySubscriptions()
                .AddSocketSessionInterceptor<TestSocketSessionInterceptor>()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");

        using var operations = new OperationManager(
            socketSession,
            new ExecutorSession(executor));
        socketSession.Operations = operations;

        var query = Utf8GraphQLParser.Parse(
            """
            subscription {
              onReview(episode: NEW_HOPE) {
                stars
              }
            }
            """);
        var request = new GraphQLRequest(query);
        var success1 = operations.Enqueue("abc", request);
        var registered1 = operations.ToArray();

        // act
        var success2 = operations.Complete("abc");
        var registered2 = operations.ToArray();

        // assert
        Assert.True(success1);
        var session = Assert.Single(registered1);
        Assert.Equal("abc", session.Id);

        Assert.True(success2);
        Assert.Empty(registered2);
    }

    [Fact]
    public async Task Complete_SessionId_Is_Null()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .AddInMemorySubscriptions()
                .AddSocketSessionInterceptor<TestSocketSessionInterceptor>()
                .BuildRequestExecutorAsync();

        var session = new Mock<ISocketSession>();
        var subscriptions = new OperationManager(session.Object, new ExecutorSession(executor));

        // act
        void Action() => subscriptions.Complete(null!);

        // assert
        Assert.Equal(
            "sessionId",
            Assert.Throws<ArgumentNullException>(Action).ParamName);
    }

    [Fact]
    public async Task Complete_SessionId_Is_Empty()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .AddInMemorySubscriptions()
                .AddSocketSessionInterceptor<TestSocketSessionInterceptor>()
                .BuildRequestExecutorAsync();

        var session = new Mock<ISocketSession>();
        var subscriptions = new OperationManager(session.Object, new ExecutorSession(executor));

        // act
        void Action() => subscriptions.Complete("");

        // assert
        Assert.Equal(
            "sessionId",
            Assert.Throws<ArgumentException>(Action).ParamName);
    }

    [Fact]
    public async Task Complete_On_Disposed_Manager()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .AddInMemorySubscriptions()
                .AddSocketSessionInterceptor<TestSocketSessionInterceptor>()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");

        var operations = new OperationManager(
            socketSession,
            new ExecutorSession(executor));
        socketSession.Operations = operations;
        operations.Dispose();

        // act
        void Fail() => operations.Complete("abc");

        // assert
        Assert.Throws<ObjectDisposedException>(Fail);
    }

    [Fact]
    public async Task Dispose_OperationManager()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .AddInMemorySubscriptions()
                .AddSocketSessionInterceptor<TestSocketSessionInterceptor>()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");

        var operations = new OperationManager(
            socketSession,
            new ExecutorSession(executor));
        socketSession.Operations = operations;

        var query = Utf8GraphQLParser.Parse(
            "subscription { onReview(episode: NEW_HOPE) { stars } }");
        var request = new GraphQLRequest(query);
        var success = operations.Enqueue("abc", request);
        Assert.True(success);

        // act
        operations.Dispose();
        var registered = operations.ToArray();

        // assert
        Assert.Empty(registered);
    }

    private class TestSocketSession : ISocketSession
    {
        public ISocketConnection Connection => throw new NotImplementedException();

        public IProtocolHandler Protocol { get; } = new TestProtocolHandler();

        public IOperationManager Operations { get; set; } = null!;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    private class TestProtocolHandler : IProtocolHandler
    {
        public string Name => "Test";

        public ValueTask OnReceiveAsync(
            ISocketSession session,
            ReadOnlySequence<byte> message,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ValueTask SendCompleteMessageAsync(
            ISocketSession session,
            string operationSessionId,
            CancellationToken cancellationToken)
            => default;

        public ValueTask SendErrorMessageAsync(
            ISocketSession session,
            string operationSessionId,
            IReadOnlyList<IError> errors,
            CancellationToken cancellationToken)
            => default;

        public ValueTask SendKeepAliveMessageAsync(
            ISocketSession session,
            CancellationToken cancellationToken)
            => default;

        public ValueTask SendResultMessageAsync(
            ISocketSession session,
            string operationSessionId,
            OperationResult result,
            CancellationToken cancellationToken)
            => default;

        public ValueTask OnConnectionInitTimeoutAsync(
            ISocketSession session,
            CancellationToken cancellationToken)
            => default;
    }

    public sealed class TestSocketSessionInterceptor : DefaultSocketSessionInterceptor
    {
        public override ValueTask OnRequestAsync(
            ISocketSession session,
            string operationSessionId,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken = default)
        {
            return default;
        }
    }
}
