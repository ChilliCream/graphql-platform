using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.AspNetCore;
using HotChocolate.AzureFunctions.IsolatedProcess;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions.Tests;

public static class DefaultGraphQLRequestExecutorIsolatedProcessExtensions
{
    public static async Task<HttpResponseData> ExecuteAsync(this IGraphQLRequestExecutor graphqlRequestExecutor, HttpRequestData httpRequestData)
    {
        if (httpRequestData is null)
            throw new ArgumentNullException(nameof(httpRequestData));

        //Adapt the Isolated Process HttpRequestData to the HttpContext needed by HotChocolate and execute the Pipeline...
        await using var httpContextShim = new GraphQLIsolatedProcessHttpContextShim(httpRequestData);

        //Marshall the HttpContext into the DefaultGraphQLRequestExecutor (which will handle pre & post processing as needed)...
        HttpContext httpContext = await httpContextShim.CreateGraphQLHttpContextAsync().ConfigureAwait(false);

        //Finally we return the result (likely an EmptyResult) as the response is already written to the HttpContext.Response!
        //NOTE: We discard the result returned as all content will be written to the HttpContext Response...
        await graphqlRequestExecutor.ExecuteAsync(httpContext.Request).ConfigureAwait(false);

        //Last, in the Isolated Process model we marshall all data back to the  HttpResponseData model and return it to the AzureFunctions process...
        //Therefore we need to marshall the Response back to the Isolated Process model...
        return await httpContextShim.CreateHttpResponseDataAsync().ConfigureAwait(false);
    }
}
