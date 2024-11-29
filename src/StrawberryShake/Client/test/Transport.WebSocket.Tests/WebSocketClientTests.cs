namespace StrawberryShake.Transport.WebSockets;

public class WebSocketClientTests
{
    [Fact]
    public void Constructor_AllArgs_CreateObject()
    {
        // arrange
        var name = "Foo";
        IReadOnlyList<ISocketProtocolFactory> protocolFactories =
            Array.Empty<ISocketProtocolFactory>();

        // act
        var exception = Record.Exception(() =>
            new WebSocketClient(name, protocolFactories));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_MonitorNull_CreateObject()
    {
        // arrange
        string name = null!;
        IReadOnlyList<ISocketProtocolFactory> protocolFactories =
            Array.Empty<ISocketProtocolFactory>();

        // act
        var exception = Record.Exception(() =>
            new WebSocketClient(name, protocolFactories));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Constructor_FactoriesNull_CreateObject()
    {
        // arrange
        var name = "Foo";
        IReadOnlyList<ISocketProtocolFactory> protocolFactories = null!;

        // act
        var exception = Record.Exception(() =>
            new WebSocketClient(name, protocolFactories));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public async Task OpenAsync_Disposed_ThrowsException()
    {
        // arrange
        var name = "Foo";
        IReadOnlyList<ISocketProtocolFactory> protocolFactories =
            Array.Empty<ISocketProtocolFactory>();
        var socket = new WebSocketClient(name, protocolFactories);
        await socket.DisposeAsync();

        // act
        var exception =
            await Record.ExceptionAsync(() => socket.OpenAsync(CancellationToken.None));

        // assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public async Task OpenAsync_UriIsNull_ThrowsException()
    {
        // arrange
        var name = "Foo";
        IReadOnlyList<ISocketProtocolFactory> protocolFactories =
            Array.Empty<ISocketProtocolFactory>();
        var socket = new WebSocketClient(name, protocolFactories);

        // act
        var exception =
            await Record.ExceptionAsync(() => socket.OpenAsync(CancellationToken.None));

        // assert
        Assert.IsType<SocketOperationException>(exception).Message.MatchSnapshot();
    }
}
