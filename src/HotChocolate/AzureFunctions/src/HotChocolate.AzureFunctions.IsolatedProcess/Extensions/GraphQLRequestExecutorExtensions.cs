using HotChocolate.AzureFunctions.IsolatedProcess;
using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions;

public static class GraphQLRequestExecutorExtensions
{
    public static Task<HttpResponseData> ExecuteAsync(
        this IGraphQLRequestExecutor executor,
        HttpRequestData requestData)
    {
        if (executor is null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        if (requestData is null)
        {
            throw new ArgumentNullException(nameof(requestData));
        }

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
