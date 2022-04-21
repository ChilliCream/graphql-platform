using System;
using System.Net.Http;
using HotChocolate.AzureFunctions.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;

namespace HotChocolate.AzureFunctions.IsolatedProcess.Tests.Helpers;

public class TestHttpRequestDataHelper
{
    public static HttpRequestData NewGraphQLHttpRequestData(IServiceProvider serviceProvider, string graphqlQuery)
    {
        HttpRequestData httpRequestData = new MockHttpRequestData(
            new MockFunctionContext(serviceProvider),
            HttpMethods.Post,
            TestHttpContextHelper.DefaultAzFuncGraphQLUri,
            requestBody: TestHttpContextHelper.CreateGraphQLRequestBody(graphqlQuery)
        );

        return httpRequestData;
    }
}
