using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Extensions;
using HotChocolate.ModelContextProtocol.Storage;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;

namespace HotChocolate.ModelContextProtocol.Handlers;

public sealed class ListToolsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithCache_UsesCache()
    {
        // arrange
        var context = await CreateRequestContextAsync();

        // act
        var result1 = await ListToolsHandler.HandleAsync(context, null, CancellationToken.None);
        var result2 = await ListToolsHandler.HandleAsync(context, null, CancellationToken.None);

        // assert
        Assert.Equal(result1, result2);
    }

    private static async Task<RequestContext<ListToolsRequestParams>> CreateRequestContextAsync()
    {
        var storage = new InMemoryMcpOperationDocumentStorage();
        await storage.SaveToolDocumentAsync(
            Utf8GraphQLParser.Parse(
                await File.ReadAllTextAsync("__resources__/GetWithNullableVariables.graphql")));
        var services = new ServiceCollection().AddSingleton<IMcpOperationDocumentStorage>(storage);
        services.AddGraphQL().AddMcp().AddQueryType<TestSchema.Query>();
        services.AddMcpServer().WithGraphQLTools();
        var serviceProvider = services.BuildServiceProvider();
        Mock<IMcpServer> mockServer = new();
        mockServer.SetupGet(s => s.Services).Returns(serviceProvider);

        return new RequestContext<ListToolsRequestParams>(mockServer.Object);
    }
}
