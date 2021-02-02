using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            ISocketOperationManager manager = new Mock<ISocketOperationManager>().Object;

            // act
            Exception? exception = Record.Exception(() => new WebSocketConnection(manager));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_ManagerNull_CreateObject()
        {
            // arrange
            ISocketOperationManager manager = null!;

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
            var managerMock = new Mock<ISocketOperationManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISocketOperationManager manager = managerMock.Object;
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
                var messageData = Encoding.UTF8.GetBytes(@"{""Foo"": ""Bar""}");
                var msg =
                    new DataDocumentOperationMessage(new ReadOnlyMemory<byte>(messageData));
                yield return msg;
            }

            var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
            var managerMock = new Mock<ISocketOperationManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISocketOperationManager manager = managerMock.Object;
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
        public async Task ExecuteAsync_DataInvalid_ParseJson()
        {
            // arrange
            async IAsyncEnumerable<OperationMessage> Producer(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var messageData = Encoding.UTF8.GetBytes(@"{""Foo""}");
                var msg =
                    new DataDocumentOperationMessage(new ReadOnlyMemory<byte>(messageData));
                yield return msg;
            }

            var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
            var managerMock = new Mock<ISocketOperationManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISocketOperationManager manager = managerMock.Object;
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
        public async Task ExecuteAsync_Error_ReturnResult()
        {
            // arrange
            async IAsyncEnumerable<OperationMessage> Producer(
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return ErrorOperationMessage.ConnectionError;
            }

            var operationRequest = new OperationRequest("foo", GetHeroQueryDocument.Instance);
            var managerMock = new Mock<ISocketOperationManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISocketOperationManager manager = managerMock.Object;
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
            var managerMock = new Mock<ISocketOperationManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISocketOperationManager manager = managerMock.Object;
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
            var managerMock = new Mock<ISocketOperationManager>();
            var operationMock = new Mock<ISocketOperation>();
            managerMock
                .Setup(x => x.StartOperationAsync(operationRequest, CancellationToken.None))
                .ReturnsAsync(operationMock.Object);
            operationMock.Setup(x => x.ReadAsync(default)).Returns(Producer());
            ISocketOperationManager manager = managerMock.Object;
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

    public class DefaultSocketClientFactoryTests
    {
        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                new Mock<IOptionsMonitor<SocketClientFactoryOptions>>().Object;
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();

            // act
            Exception? exception = Record.Exception(() =>
                new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_MonitorNull_CreateObject()
        {
            // arrange
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor = null!;
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();

            // act
            Exception? exception = Record.Exception(() =>
                new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void Constructor_FactoriesNull_CreateObject()
        {
            // arrange
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                new Mock<IOptionsMonitor<SocketClientFactoryOptions>>().Object;
            IEnumerable<ISocketProtocolFactory> protocolFactories = null!;

            // act
            Exception? exception = Record.Exception(() =>
                new DefaultSocketClientFactory(optionsMonitor, protocolFactories));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void CreateClient_OptionsRegistered_CreateClient()
        {
            // arrange
            ServiceProvider sp = new ServiceCollection()
                .Configure<SocketClientFactoryOptions>(
                    "Foo",
                    x => { })
                .BuildServiceProvider();
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();
            var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

            // act
            ISocketClient? client = factory.CreateClient("Foo");

            // assert
            Assert.IsType<WebSocketClient>(client);
        }

        [Fact]
        public void CreateClient_OptionsRegistered_ApplyConfig()
        {
            // arrange
            var uri = new Uri("wss://localhost:123");
            ServiceProvider sp = new ServiceCollection()
                .Configure<SocketClientFactoryOptions>(
                    "Foo",
                    x => x.SocketClientActions.Add(x => x.Uri = uri))
                .BuildServiceProvider();
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();
            var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

            // act
            ISocketClient? client = factory.CreateClient("Foo");

            // assert
            Assert.Equal(uri, client.Uri);
        }

        [Fact]
        public void CreateClient_NoOptionsRegistered_CreateClient()
        {
            // arrange
            ServiceProvider sp = new ServiceCollection()
                .Configure<SocketClientFactoryOptions>(
                    "Foo",
                    x => { })
                .BuildServiceProvider();
            IOptionsMonitor<SocketClientFactoryOptions> optionsMonitor =
                sp.GetRequiredService<IOptionsMonitor<SocketClientFactoryOptions>>();
            IEnumerable<ISocketProtocolFactory> protocolFactories =
                Enumerable.Empty<ISocketProtocolFactory>();
            var factory = new DefaultSocketClientFactory(optionsMonitor, protocolFactories);

            // act
            ISocketClient? client = factory.CreateClient("Baz");

            // assert
            Assert.IsType<WebSocketClient>(client);
        }
    }
}
