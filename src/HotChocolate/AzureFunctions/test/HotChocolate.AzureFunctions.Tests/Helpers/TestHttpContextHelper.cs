using System;
using HotChocolate.AzureFunctions.IsolatedProcess.Extensions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Net.Http.Headers;
using IO = System.IO;

namespace HotChocolate.AzureFunctions.Tests.Helpers;

public class TestHttpContextHelper
{
    public static Uri DefaultAzFuncGraphQLUri { get; } = new(
        new Uri("https://localhost/"),
        GraphQLAzureFunctionsConstants.DefaultGraphQLRoute
    );

    public static HttpContext NewGraphQLHttpContext(
        IServiceProvider serviceProvider,
        string graphqlQuery)
    {
        var httpContext = new HttpContextBuilder()
            .CreateHttpContext(
                HttpMethods.Post,
                DefaultAzFuncGraphQLUri,
                requestBody: CreateGraphQLRequestBody(graphqlQuery));

        // Ensure we accept Json for GraphQL requests...
        httpContext.Request.Headers.Add(
            HeaderNames.Accept,
            GraphQLAzureFunctionsConstants.DefaultJsonContentType);

        // Ensure that we enable support for HttpContext injection for Unit Tests
        serviceProvider.SetCurrentHttpContext(httpContext);

        return httpContext;
    }

    public static HttpContext NewBcpHttpContext(IServiceProvider serviceProvider, string path)
    {
        var httpContext = new HttpContextBuilder()
            .CreateHttpContext(
                HttpMethods.Get,
                new Uri(IO.Path.Combine(DefaultAzFuncGraphQLUri.ToString(), path)),
                requestBodyContentType: GraphQLAzureFunctionsConstants.DefaultBcpContentType);

        // Ensure we accept Text/Html for BCP requests...
        httpContext.Request.Headers.Add(
            HeaderNames.Accept,
            GraphQLAzureFunctionsConstants.DefaultBcpContentType);

        // Ensure that we enable support for HttpContext injection for Unit Tests
        serviceProvider.SetCurrentHttpContext(httpContext);

        return httpContext;
    }

    public static string CreateGraphQLRequestBody(string graphQLQuery)
        => JObject.FromObject(new { query = graphQLQuery }).ToString(Formatting.Indented);
}
