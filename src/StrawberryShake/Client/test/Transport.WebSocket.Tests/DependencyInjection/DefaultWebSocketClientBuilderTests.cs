using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.WebSockets;

public class DefaultWebSocketClientBuilderTests
{
    [Fact]
    public void Constructor_AllArgsProvided_NotThrow()
    {
        // arrange
        var services = new ServiceCollection();
        var name = "Foo";

        // act
        var builder = new DefaultWebSocketClientBuilder(services, name);

        // assert
        Assert.IsType<DefaultWebSocketClientBuilder>(builder);
    }

    [Fact]
    public void Constructor_ServicesNull_ThrowError()
    {
        // arrange
        var name = "Foo";

        // act
        var ex = Record.Exception(() => new DefaultWebSocketClientBuilder(null!, name));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void Constructor_NameNull_ThrowError()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        var ex =
            Record.Exception(() => new DefaultWebSocketClientBuilder(services, null!));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }
}
