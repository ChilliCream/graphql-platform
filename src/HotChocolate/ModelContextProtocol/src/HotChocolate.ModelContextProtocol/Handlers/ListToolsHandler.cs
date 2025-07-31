using HotChocolate.Execution;
using HotChocolate.ModelContextProtocol.Caching;
using HotChocolate.ModelContextProtocol.Factories;
using HotChocolate.ModelContextProtocol.Storage;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HotChocolate.ModelContextProtocol.Handlers;

internal static class ListToolsHandler
{
    public static async ValueTask<ListToolsResult> HandleAsync(
        RequestContext<ListToolsRequestParams> context,
        string? schemaName,
        CancellationToken cancellationToken)
    {
        var cache = context.Services!.GetRequiredService<IListToolsCache>();

        if (cache.TryGetValue(out var cachedResult))
        {
            return cachedResult;
        }

        var storage = context.Services!.GetRequiredService<IMcpOperationDocumentStorage>();
        var toolDocuments = await storage.GetToolDocumentsAsync(cancellationToken);
        var requestExecutor =
            await context.Services!
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(schemaName, cancellationToken);
        var toolFactory = new ToolFactory(requestExecutor.Schema);
        List<Tool> tools = [];

        foreach (var (name, document) in toolDocuments)
        {
            tools.Add(toolFactory.CreateTool(name, document));
        }

        var listToolsResult = new ListToolsResult
        {
            Tools = tools
        };

        cache.Set(listToolsResult);

        return listToolsResult;
    }
}
