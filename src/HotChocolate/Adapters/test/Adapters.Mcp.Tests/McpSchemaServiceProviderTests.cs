using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.Mcp;

public sealed class McpSchemaServiceProviderTests
{
    [Fact]
    public void GetService_NotBound_ThrowsInvalidOperationException()
    {
        // arrange
        var schemaServices = new McpSchemaServiceProvider();

        // act
        void Act() => schemaServices.GetService(typeof(TestService));

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal("The MCP schema services have not been bound.", exception.Message);
    }

    [Fact]
    public void GetService_Bound_ReturnsServiceFromBoundProvider()
    {
        // arrange
        var expectedService = new TestService();
        using var serviceProvider = new ServiceCollection()
            .AddSingleton(expectedService)
            .BuildServiceProvider();
        var schemaServices = new McpSchemaServiceProvider();
        schemaServices.Bind(serviceProvider);

        // act
        var service = schemaServices.GetService(typeof(TestService));

        // assert
        Assert.Same(expectedService, service);
    }

    private sealed class TestService;
}
