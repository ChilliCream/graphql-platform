using HotChocolate.Language;
using Moq;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets;

public class SocketOperationTests
{
    [Fact]
    public void Constructor_SingleArgs_CreateObject()
    {
        // arrange
        var manager =
            new Mock<ISession>().Object;

        // act
        var exception = Record.Exception(() =>
            new SocketOperation(manager));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_SingleArgs_CreateUniqueId()
    {
        // arrange
        var manager =
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
        var manager =
            new Mock<ISession>().Object;
        var id = "123";

        // act
        var exception = Record.Exception(() =>
            new SocketOperation(manager, id));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_MonitorNull_CreateObject()
    {
        // arrange
        ISession manager = null!;
        var id = "123";

        // act
        var exception = Record.Exception(() =>
            new SocketOperation(manager, id));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Constructor_FactoriesNull_CreateObject()
    {
        // arrange
        var manager =
            new Mock<ISession>().Object;
        string id = null!;

        // act
        var exception = Record.Exception(() =>
            new SocketOperation(manager, id));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task ReadAsync_IsDisposed_Return()
    {
        // arrange
        var manager = new Mock<ISession>().Object;
        const string id = "123";
        var operation = new SocketOperation(manager, id);
        await operation.DisposeAsync();
        List<OperationMessage> messages = [];

        // act
        await foreach (var elm in operation.ReadAsync())
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
        var manager = new Mock<ISession>().Object;
        const string id = "123";
        var operation = new SocketOperation(manager, id);
        List<OperationMessage> messages = [];

        // act
        await operation.ReceiveMessageAsync(
            ErrorOperationMessage.ConnectionInitializationError,
            CancellationToken.None);

        await foreach (var elm in operation.ReadAsync())
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
        var id = "123";
        managerMock
            .Setup(x => x.StopOperationAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var manager = managerMock.Object;
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
        var id = "123";
        managerMock
            .Setup(x => x.StopOperationAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var manager = managerMock.Object;
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
