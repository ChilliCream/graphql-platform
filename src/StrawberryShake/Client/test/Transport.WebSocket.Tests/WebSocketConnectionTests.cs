using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Snapshooter.Xunit;
using StrawberryShake.Http.Subscriptions;
using StrawberryShake.Transport.WebSockets.Messages;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class WebSocketConnectionTests
    {
        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            ISessionManager manager = new Mock<ISessionManager>().Object;

            // act
            Exception? exception = Record.Exception(() => new WebSocketConnection(manager));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_ManagerNull_CreateObject()
        {
            // arrange
            ISessionManager manager = null!;

            // act
            Exception? exception = Record.Exception(() => new WebSocketConnection(manager));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task ExecuteAsync_Completed_Complete()
        {
            // arrange
            async IAsyncEnumerable<OperationMessage> Producer(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield break;
            }

            var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
            var managerMock = new Mock<ISessionManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISessionManager manager = managerMock.Object;
            var connection = new WebSocketConnection(manager);
            var results = new List<Response<JsonDocument>>();

            // act
            await foreach (var response in connection.ExecuteAsync(operationRequest))
            {
                results.Add(response);
            }

            // assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task ExecuteAsync_Data_ParseJson()
        {
            // arrange
            async IAsyncEnumerable<OperationMessage> Producer(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var messageData = JsonDocument.Parse(@"{""Foo"": ""Bar""}");
                var msg =
                    new DataDocumentOperationMessage<JsonDocument>(messageData);
                yield return msg;
            }

            var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
            var managerMock = new Mock<ISessionManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISessionManager manager = managerMock.Object;
            var connection = new WebSocketConnection(manager);
            var results = new List<Response<JsonDocument>>();

            // act
            await foreach (var response in connection.ExecuteAsync(operationRequest))
            {
                results.Add(response);
            }

            // assert
            Assert.Single(results)!.Body!.RootElement!.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Error_ReturnResult()
        {
            // arrange
            async IAsyncEnumerable<OperationMessage> Producer(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return ErrorOperationMessage.ConnectionInitializationError;
            }

            var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
            var managerMock = new Mock<ISessionManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISessionManager manager = managerMock.Object;
            var connection = new WebSocketConnection(manager);
            var results = new List<Response<JsonDocument>>();

            // act
            await foreach (var response in connection.ExecuteAsync(operationRequest))
            {
                results.Add(response);
            }

            // assert
            Response<JsonDocument>? res = Assert.Single(results);
            res?.Exception?.Message.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Cancelled_ReturnResult()
        {
            // arrange
            async IAsyncEnumerable<OperationMessage> Producer(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return CancelledOperationMessage.Default;
            }

            var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
            var managerMock = new Mock<ISessionManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISessionManager manager = managerMock.Object;
            var connection = new WebSocketConnection(manager);
            var results = new List<Response<JsonDocument>>();

            // act
            await foreach (var response in connection.ExecuteAsync(operationRequest))
            {
                results.Add(response);
            }

            // assert
            Response<JsonDocument>? res = Assert.Single(results);
            res?.Exception?.Message.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Completed_ReturnResult()
        {
            // arrange
            async IAsyncEnumerable<OperationMessage> Producer(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return CompleteOperationMessage.Default;
            }

            var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
            var managerMock = new Mock<ISessionManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISessionManager manager = managerMock.Object;
            var connection = new WebSocketConnection(manager);
            var results = new List<Response<JsonDocument>>();

            // act
            await foreach (var response in connection.ExecuteAsync(operationRequest))
            {
                results.Add(response);
            }

            // assert
            Assert.Empty(results);
        }

        private class GetHeroQueryDocument : IDocument
        {
            private const string _bodyString =
                @"query GetHero {
                hero {
                    __typename
                    id
                    name
                    friends {
                        nodes {
                            __typename
                            id
                            name
                        }
                        totalCount
                    }
                }
                version
            }";

            private static readonly byte[] _body = Encoding.UTF8.GetBytes(_bodyString);

            private GetHeroQueryDocument() { }

            public OperationKind Kind => OperationKind.Query;

            public ReadOnlySpan<byte> Body => _body;

            public override string ToString() => _bodyString;

            public static GetHeroQueryDocument Instance { get; } = new();
        }
    }
}
