using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Moq;
using StrawberryShake.Transport.WebSockets.Messages;
using Xunit;

namespace StrawberryShake.Transport.WebSockets
{
    public class SocketOperationTests
    {
        [Fact]
        public void Constructor_SingleArgs_CreateObject()
        {
            // arrange
            ISession manager =
                new Mock<ISession>().Object;

            // act
            Exception? exception = Record.Exception(() =>
                new SocketOperation(manager));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_SingleArgs_CreateUniqueId()
        {
            // arrange
            ISession manager =
                new Mock<ISession>().Object;

            // act
            var first = new SocketOperation(manager);
            var second = new SocketOperation(manager);

            // assert
            Assert.NotNull(first.Id);
            Assert.NotNull(second.Id);
            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public void Constructor_AllArgs_CreateObject()
        {
            // arrange
            ISession manager =
                new Mock<ISession>().Object;
            string id = "123";

            // act
            Exception? exception = Record.Exception(() =>
                new SocketOperation(manager, id));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_MonitorNull_CreateObject()
        {
            // arrange
            ISession manager = null!;
            string id = "123";

            // act
            Exception? exception = Record.Exception(() =>
                new SocketOperation(manager, id));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void Constructor_FactoriesNull_CreateObject()
        {
            // arrange
            ISession manager =
                new Mock<ISession>().Object;
            string id = null!;

            // act
            Exception? exception = Record.Exception(() =>
                new SocketOperation(manager, id));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public async Task ReadAsync_IsDisposed_Return()
        {
            // arrange
            ISession manager = new Mock<ISession>().Object;
            string id = "123";
            var operation = new SocketOperation(manager, id);
            await operation.DisposeAsync();
            List<OperationMessage> messages = new();

            // act
            await foreach (var elm in operation.ReadAsync(CancellationToken.None))
            {
                messages.Push(elm);
            }

            // assert
            Assert.Empty(messages);
        }

        [Fact]
        public async Task ReadAsync_IsDisposedDuringReceiving_Return()
        {
            // arrange
            ISession manager = new Mock<ISession>().Object;
            string id = "123";
            var operation = new SocketOperation(manager, id);
            List<OperationMessage> messages = new();

            // act
            await operation.ReceiveMessageAsync(
                ErrorOperationMessage.ConnectionInitializationError,
                CancellationToken.None);

            await foreach (var elm in operation.ReadAsync(CancellationToken.None))
            {
                messages.Push(elm);
                await operation.DisposeAsync();
            }

            // assert
            Assert.Single(messages);
        }

        [Fact]
        public async Task Dispose_IsNotDisposed_StopOperationAsync()
        {
            // arrange
            var managerMock = new Mock<ISession>(MockBehavior.Strict);
            string id = "123";
            managerMock
                .Setup(x => x.StopOperationAsync(id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            ISession manager = managerMock.Object;
            var operation = new SocketOperation(manager, id);

            // act
            await operation.DisposeAsync();

            // assert
            managerMock.VerifyAll();
        }

        [Fact]
        public async Task Dispose_IsDisposed_StopOperationAsync()
        {
            // arrange
            var managerMock = new Mock<ISession>(MockBehavior.Strict);
            string id = "123";
            managerMock
                .Setup(x => x.StopOperationAsync(id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            ISession manager = managerMock.Object;
            var operation = new SocketOperation(manager, id);

            // act
            await operation.DisposeAsync();
            await operation.DisposeAsync();
            await operation.DisposeAsync();

            // assert
            managerMock
                .Verify(x => x.StopOperationAsync(id, It.IsAny<CancellationToken>()),
                    Times.Once);
        }
    }
}
