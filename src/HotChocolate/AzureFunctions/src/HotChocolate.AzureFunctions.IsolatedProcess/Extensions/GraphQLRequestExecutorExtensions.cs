using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

public static class GraphQLRequestExecutorExtensions
{
    public static async Task<HttpResponseData> ExecuteAsync(this IGraphQLRequestExecutor graphqlRequestExecutor, HttpRequestData httpRequestData)
    {
        if (graphqlRequestExecutor is null)
            throw new ArgumentNullException(nameof(graphqlRequestExecutor));

        if (httpRequestData is null)    
            throw new ArgumentNullException(nameof(httpRequestData));
        
        //Adapt the Isolated Process HttpRequestData to the HttpContext needed by HotChocolate and execute the Pipeline...
        //NOTE: This must be disposed of properly to ensure our request/response resources are managed efficiently.
        await using var httpContextShim = new GraphQLIsolatedProcessHttpContextShim(httpRequestData);

        //Marshall the HttpContext into the DefaultGraphQLRequestExecutor (which will handle pre & post processing as needed)...
        HttpContext httpContext = await httpContextShim.CreateGraphQLHttpContextAsync().ConfigureAwait(false);

        //Now we can execute the request with a valid HttpContext...
        //NOTE: We discard the result returned (likely an EmptyResult) as all content is already written to the HttpContext Response.
        await graphqlRequestExecutor.ExecuteAsync(httpContext.Request).ConfigureAwait(false);

        //Last, in the Isolated Process model we marshall all data back to the  HttpResponseData model and return it to the AzureFunctions process...
        //Therefore we need to marshall the Response back to the Isolated Process model...
        return await httpContextShim.CreateHttpResponseDataAsync().ConfigureAwait(false);
    }
}
