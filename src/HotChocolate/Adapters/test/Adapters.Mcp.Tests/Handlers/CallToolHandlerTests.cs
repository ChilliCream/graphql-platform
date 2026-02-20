using HotChocolate.Adapters.Mcp.Extensions;
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace HotChocolate.Adapters.Mcp.Handlers;

public sealed class CallToolHandlerTests
{
    [Fact]
    public async Task HandleAsync_MissingTool_ReturnsCallToolResultWithError()
    {
        // arrange
        var context = await CreateRequestContextAsync("unknown");

        // act
        var result = await CallToolHandler.HandleAsync(context, CancellationToken.None);

        // assert
        Assert.True(result.IsError);
        var textContentBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Equal("The tool 'unknown' was not found.", textContentBlock.Text);
    }

    private static async Task<RequestContext<CallToolRequestParams>> CreateRequestContextAsync(
        string toolName)
    {
        var storage = new TestMcpStorage();
        await storage.AddOrUpdateToolAsync(
            new OperationToolDefinition(
                Utf8GraphQLParser.Parse(
                    await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql"))));
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddGraphQL()
            .AddAuthorization()
            .AddMcp()
            .AddMcpStorage(storage)
            .AddQueryType<TestSchema.Query>()
            .AddInterfaceType<TestSchema.IPet>()
            .AddUnionType<TestSchema.IPet>()
            .AddObjectType<TestSchema.Cat>()
            .AddObjectType<TestSchema.Dog>();
        var serviceProvider = services.BuildServiceProvider();
        var executorProvider = serviceProvider.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync();
        Mock<McpServer> mockServer = new();
        mockServer.SetupGet(s => s.Services).Returns(executor.Schema.Services);
        var request = new JsonRpcRequest { Method = RequestMethods.ToolsCall };

        return new RequestContext<CallToolRequestParams>(mockServer.Object, request)
        {
            Params = new CallToolRequestParams
            {
                Name = toolName
            }
        };
    }
}
