using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

public static class GraphQLRequestExecutorExtensions
{
    public static Task<HttpResponseData> ExecuteAsync(
        this IGraphQLRequestExecutor graphqlRequestExecutor,
        HttpRequestData httpRequestData)
    {
        if (graphqlRequestExecutor is null)
        {
            throw new ArgumentNullException(nameof(graphqlRequestExecutor));
        }

        if (httpRequestData is null)
        {
            throw new ArgumentNullException(nameof(httpRequestData));
        }

        // Factored out Async logic to Address SonarCloud concern for exceptions in Async flow ...
        return ExecuteGraphQLRequestInternalAsync(graphqlRequestExecutor, httpRequestData);
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
