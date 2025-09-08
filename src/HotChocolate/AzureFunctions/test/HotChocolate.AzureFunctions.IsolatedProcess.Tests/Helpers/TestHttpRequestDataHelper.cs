using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using static HotChocolate.AzureFunctions.Tests.Helpers.TestHttpContextHelper;
using static Microsoft.Net.Http.Headers.HeaderNames;
using IO = System.IO;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;

public static class TestHttpRequestDataHelper
{
    public static HttpRequestData NewGraphQLHttpRequestData(
        IServiceProvider serviceProvider,
        string graphqlQuery)
    {
        HttpRequestData httpRequestData = new MockHttpRequestData(
            new MockFunctionContext(serviceProvider),
            HttpMethods.Post,
            DefaultAzFuncGraphQLUri,
            requestBody: CreateRequestBody(graphqlQuery));

        //Ensure we accept Json for GraphQL requests...
        httpRequestData.Headers.Add(Accept, TestConstants.DefaultJsonContentType);

        return httpRequestData;
    }

    public static HttpRequestData NewNitroHttpRequestData(
        IServiceProvider serviceProvider,
        string path)
    {
        HttpRequestData httpRequestData = new MockHttpRequestData(
            new MockFunctionContext(serviceProvider),
            HttpMethods.Get,
            new Uri(IO.Path.Combine(DefaultAzFuncGraphQLUri.ToString(), path)));

        //Ensure we accept Text/Html for Nitro requests...
        httpRequestData.Headers.Add(Accept, TestConstants.DefaultNitroContentType);

        return httpRequestData;
    }
}
