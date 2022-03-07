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
        void Action() => subscriptions.Register(null!, new GraphQLRequest(null, queryId: "123"));

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
        void Action() => subscriptions.Register("", new GraphQLRequest(null, queryId: "123"));

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
        void Action() => subscriptions.Register("abc", null!);

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

        using var subscriptions = new OperationManager(
            socketSession,
            new DefaultSocketSessionInterceptor(),
            executor);

        var query = Utf8GraphQLParser.Parse("{ hero(id: 1) { name } }");
        var request = new GraphQLRequest(query);

        // act
        var success = subscriptions.Register("abc", request);
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

        using var subscriptions = new OperationManager(
            socketSession,
            new DefaultSocketSessionInterceptor(),
            executor);

        var query = Utf8GraphQLParser.Parse("{ hero(id: 1) { name } }");
        var request = new GraphQLRequest(query);
        var success1 = subscriptions.Register("abc", request);
        var registered1 = subscriptions.ToArray();

        // act
        var success2 = subscriptions.Register("abc", request);
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

        using var subscriptions = new OperationManager(
            socketSession,
            new DefaultSocketSessionInterceptor(),
            executor);

        var query = Utf8GraphQLParser.Parse("{ hero(id: 1) { name } }");
        var request = new GraphQLRequest(query);
        var success1 = subscriptions.Register("abc", request);
        var registered1 = subscriptions.ToArray();

        // act
        var success2 = subscriptions.Unregister("abc");
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

        public Task OnReceiveAsync(
            ISocketSession session,
            ReadOnlySequence<byte> message,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SendCompleteMessageAsync(
            ISocketSession session,
            string operationSessionId,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SendErrorMessageAsync(
            ISocketSession session,
            string operationSessionId,
            IReadOnlyList<IError> errors,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SendKeepAliveMessageAsync(
            ISocketSession session,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SendResultMessageAsync(
            ISocketSession session,
            string operationSessionId,
            IQueryResult result,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task OnConnectionInitTimeoutAsync(
            ISocketSession session,
            CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
