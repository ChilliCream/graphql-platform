using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions;

[Collection("Sockets")]
public class SubscriptionManagerTests
{
    [Fact]
    public async Task Register_SessionId_Is_Null()
    {
        // arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .BuildRequestExecutorAsync();

        var session = new Mock<ISocketSession>();
        var interceptor = new Mock<ISocketSessionInterceptor>();
        var subscriptions = new OperationManager(session.Object, interceptor.Object, executor);

        // act
        void Action() => subscriptions.Enqueue(null!, new GraphQLRequest(null, queryId: "123"));

        // assert
        Assert.Equal(
            "sessionId",
            Assert.Throws<ArgumentException>(Action).ParamName);
    }

    [Fact]
    public async Task Register_SessionId_Is_Empty()
    {
        // arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .BuildRequestExecutorAsync();

        var session = new Mock<ISocketSession>();
        var interceptor = new Mock<ISocketSessionInterceptor>();
        var subscriptions = new OperationManager(session.Object, interceptor.Object, executor);

        // act
        void Action() => subscriptions.Enqueue("", new GraphQLRequest(null, queryId: "123"));

        // assert
        Assert.Equal(
            "sessionId",
            Assert.Throws<ArgumentException>(Action).ParamName);
    }

    [Fact]
    public async Task Register_Request_Is_Null()
    {
        // arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .BuildRequestExecutorAsync();

        var session = new Mock<ISocketSession>();
        var interceptor = new Mock<ISocketSessionInterceptor>();
        var subscriptions = new OperationManager(session.Object, interceptor.Object, executor);

        // act
        void Action() => subscriptions.Enqueue("abc", null!);

        // assert
        Assert.Equal(
            "request",
            Assert.Throws<ArgumentNullException>(Action).ParamName);
    }

    [Fact]
    public async Task Register_Request()
    {
        // arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");
        
        using var subscriptions = new OperationManager(
            socketSession,
            new DefaultSocketSessionInterceptor(),
            executor,
            _ => mockSession.Object);

        var query = Utf8GraphQLParser.Parse(
            "subscription { onReview(episode: NEW_HOPE) { stars } }");
        var request = new GraphQLRequest(query);

        // act
        var success = subscriptions.Enqueue("abc", request);
        var registered = subscriptions.ToArray();

        // assert
        Assert.True(success);
        Assert.Collection(registered, t => Assert.Equal("abc", t.Id));
    }

    [Fact]
    public async Task Unregister_Request_With_Non_Unique_Id()
    {
        // arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");

        using var subscriptions = new OperationManager(
            socketSession,
            new DefaultSocketSessionInterceptor(),
            executor,
            _ => mockSession.Object);

        var query = Utf8GraphQLParser.Parse(
            "subscription { onReview(episode: NEW_HOPE) { stars } }");
        var request = new GraphQLRequest(query);
        var success1 = subscriptions.Enqueue("abc", request);
        var registered1 = subscriptions.ToArray();

        // act
        var success2 = subscriptions.Enqueue("abc", request);
        var registered2 = subscriptions.ToArray();

        // assert
        Assert.True(success1);
        Assert.Collection(registered1, t => Assert.Equal("abc", t.Id));
        Assert.False(success2);
        Assert.Collection(registered2, t => Assert.Equal("abc", t.Id));
    }

    [Fact]
    public async Task Unregister_Request()
    {
        // arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddStarWars()
                .BuildRequestExecutorAsync();

        var socketSession = new TestSocketSession();

        var mockSession = new Mock<IOperationSession>();
        mockSession.SetupGet(t => t.Id).Returns("abc");

        using var subscriptions = new OperationManager(
            socketSession,
            new DefaultSocketSessionInterceptor(),
            executor,
            _ => mockSession.Object);

        var query = Utf8GraphQLParser.Parse(
            "subscription { onReview(episode: NEW_HOPE) { stars } }");
        var request = new GraphQLRequest(query);
        var success1 = subscriptions.Enqueue("abc", request);
        var registered1 = subscriptions.ToArray();

        // act
        var success2 = subscriptions.Complete("abc");
        var registered2 = subscriptions.ToArray();

        // assert
        Assert.True(success1);
        Assert.Collection(registered1, t => Assert.Equal("abc", t.Id));
        Assert.True(success2);
        Assert.Empty(registered2);
    }

    private class TestSocketSession : ISocketSession
    {
        public ISocketConnection Connection => throw new NotImplementedException();

        public IProtocolHandler Protocol { get; } = new TestProtocolHandler();

        public IOperationManager Operations => throw new NotImplementedException();

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
            IQueryResult result,
            CancellationToken cancellationToken)
            => default;

        public ValueTask OnConnectionInitTimeoutAsync(
            ISocketSession session,
            CancellationToken cancellationToken)
            => default;
    }
}
