using System;
using HotChocolate.AzureFunctions.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using static Microsoft.Net.Http.Headers.HeaderNames;
using IO = System.IO;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;

public class TestHttpRequestDataHelper
{
    public static HttpRequestData NewGraphQLHttpRequestData(
        IServiceProvider serviceProvider,
        string graphqlQuery)
    {
        HttpRequestData httpRequestData = new MockHttpRequestData(
            new MockFunctionContext(serviceProvider),
            HttpMethods.Post,
            TestHttpContextHelper.DefaultAzFuncGraphQLUri,
            requestBody: TestHttpContextHelper.CreateGraphQLRequestBody(graphqlQuery));

        //Ensure we accept Json for GraphQL requests...
        httpRequestData.Headers.Add(Accept, GraphQLAzureFunctionsConstants.DefaultJsonContentType);

        return httpRequestData;
    }

    public static HttpRequestData NewBcpHttpRequestData(
        IServiceProvider serviceProvider,
        string path)
    {
        HttpRequestData httpRequestData = new MockHttpRequestData(
            new MockFunctionContext(serviceProvider),
            HttpMethods.Get,
            new Uri(IO.Path.Combine(TestHttpContextHelper.DefaultAzFuncGraphQLUri.ToString(), path))
        );

        //Ensure we accept Text/Html for BCP requests...
        httpRequestData.Headers.Add(Accept, GraphQLAzureFunctionsConstants.DefaultBcpContentType);

        return httpRequestData;
    }
}
