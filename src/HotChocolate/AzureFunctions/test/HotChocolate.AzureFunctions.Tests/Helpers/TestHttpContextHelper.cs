using System;
using HotChocolate.AzureFunctions.IsolatedProcess.Extensions;
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

    public static HttpContext NewGraphQLHttpContext(IServiceProvider serviceProvider, string graphqlQuery)
    {
        var httpContext = new HttpContextBuilder()
            .CreateHttpContext(
                HttpMethods.Post,
                DefaultAzFuncGraphQLUri,
                requestBody: CreateGraphQLRequestBody(graphqlQuery)
            );

        //Ensure that we enable support for HttpContext injection for Unit Tests
        serviceProvider.SetCurrentHttpContext(httpContext);

        return httpContext;
    }

    public static string CreateGraphQLRequestBody(string graphQLQuery)
    {
        return JObject.FromObject(new
        {
            query = graphQLQuery
        }).ToString(Formatting.Indented);
    }
}
