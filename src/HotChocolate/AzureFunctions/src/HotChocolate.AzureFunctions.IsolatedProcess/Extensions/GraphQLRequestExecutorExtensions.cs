using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

public static class GraphQLRequestExecutorExtensions
{
    public static Task<HttpResponseData> ExecuteAsync(this IGraphQLRequestExecutor graphqlRequestExecutor, HttpRequestData httpRequestData)
    {
        if (graphqlRequestExecutor is null)
            throw new ArgumentNullException(nameof(graphqlRequestExecutor));

        if (httpRequestData is null)    
            throw new ArgumentNullException(nameof(httpRequestData));
        
        //Factored out Async logic to Address Sonarrcloud concern for exceptions in Async flow...
        return ExecuteGraphQLRequestInternalAsync(graphqlRequestExecutor, httpRequestData);
    }

    private static async Task<HttpResponseData> ExecuteGraphQLRequestInternalAsync(IGraphQLRequestExecutor graphqlRequestExecutor, HttpRequestData httpRequestData)
    {
        //Adapt the Isolated Process HttpRequestData to the HttpContext needed by HotChocolate and execute the Pipeline...
        //NOTE: This must be disposed of properly to ensure our request/response resources are managed efficiently.
        await using HttpContextShim httpContextShim = await HttpContextShim.CreateHttpContextAsync(httpRequestData).ConfigureAwait(false);

        //Now we can execute the request by marshalling the HttpContext into the DefaultGraphQLRequestExecutor (which will handle pre & post processing as needed)...
        //NOTE: We discard the result returned (likely an EmptyResult) as all content is already written to the HttpContext Response.
        await graphqlRequestExecutor.ExecuteAsync(httpContextShim.HttpContext.Request).ConfigureAwait(false);

        //Last, in the Isolated Process model we marshall all data back to the  HttpResponseData model and return it to the AzureFunctions process...
        //Therefore we need to marshall the Response back to the Isolated Process model...
        return await httpContextShim.CreateHttpResponseDataAsync().ConfigureAwait(false);
    }
}
