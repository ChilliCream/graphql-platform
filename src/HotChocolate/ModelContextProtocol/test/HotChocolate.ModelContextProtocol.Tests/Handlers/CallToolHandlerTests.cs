using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.ModelContextProtocol.Storage;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace HotChocolate.ModelContextProtocol.Handlers;

public sealed class CallToolHandlerTests
{
    [Fact]
    public async Task HandleAsync_MissingTool_ReturnsCallToolResultWithError()
    {
        // arrange
        var context = await CreateRequestContextAsync("unknown");

        // act
        var result = await CallToolHandler.HandleAsync(context, null, CancellationToken.None);

        // assert
        Assert.True(result.IsError);
        var textContentBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Equal("The tool 'unknown' was not found.", textContentBlock.Text);
    }

    private static async Task<RequestContext<CallToolRequestParams>> CreateRequestContextAsync(
        string toolName)
    {
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql")));
        var services = new ServiceCollection().AddSingleton<IMcpOperationDocumentStorage>(storage);
        services
            .AddGraphQL()
            .AddMcp()
            .AddQueryType<TestSchema.Query>()
            .AddInterfaceType<TestSchema.IPet>()
            .AddUnionType<TestSchema.IPet>()
            .AddObjectType<TestSchema.Cat>()
            .AddObjectType<TestSchema.Dog>();
        services
            .AddMcpServer()
            .WithGraphQLTools();
        var serviceProvider = services.BuildServiceProvider();
        Mock<IMcpServer> mockServer = new();
        mockServer.SetupGet(s => s.Services).Returns(serviceProvider);

        return new RequestContext<CallToolRequestParams>(mockServer.Object)
        {
            Params = new CallToolRequestParams
            {
                Name = toolName
            }
        };
    }
}
