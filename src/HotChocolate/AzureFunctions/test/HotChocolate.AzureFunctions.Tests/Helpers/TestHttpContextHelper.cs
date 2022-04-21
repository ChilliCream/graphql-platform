using System;
using HotChocolate.AzureFunctions.IsolatedProcess;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AzureFunctions.Tests.Helpers;
public class TestHttpContextHelper
{
    public static Uri DefaultAzFuncGraphQLUri { get; } = new(
        new Uri("https://localhost/"),
        GraphQLAzureFunctionsConstants.DefaultGraphQLRoute
    );

    public static HttpContext NewGraphQLHttpContext(string? graphqlQuery)
        => new HttpContextBuilder().CreateHttpContext(HttpMethods.Post, DefaultAzFuncGraphQLUri, requestBody: CreateGraphQLRequestBody(graphqlQuery));

    public static string CreateGraphQLRequestBody(string graphQLQuery)
    {
        return JObject.FromObject(new
        {
            query = graphQLQuery
        }).ToString(Formatting.Indented);
    }
}
