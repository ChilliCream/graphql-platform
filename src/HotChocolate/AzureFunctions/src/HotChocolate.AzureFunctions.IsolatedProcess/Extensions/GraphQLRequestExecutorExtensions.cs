using HotChocolate.AzureFunctions.IsolatedProcess;
using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions;

public static class GraphQLRequestExecutorExtensions
{
    public static Task<HttpResponseData> ExecuteAsync(
        this IGraphQLRequestExecutor executor,
        HttpRequestData requestData)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(requestData);

        return ExecuteGraphQLRequestInternalAsync(executor, requestData);
    }

    private static async Task<HttpResponseData> ExecuteGraphQLRequestInternalAsync(
        IGraphQLRequestExecutor executor,
        HttpRequestData requestData)
    {
        var context = new AzureHttpContext(requestData);
        await executor.ExecuteAsync(context).ConfigureAwait(false);
        return context.CreateResponseData();
    }
}
