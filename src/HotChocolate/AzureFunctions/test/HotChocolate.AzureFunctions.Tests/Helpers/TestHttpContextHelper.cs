using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Net.Http.Headers;
using IO = System.IO;

namespace HotChocolate.AzureFunctions.Tests.Helpers;

public static class TestHttpContextHelper
{
    public static Uri DefaultAzFuncGraphQLUri { get; } = new(
        new Uri("https://localhost/"),
        GraphQLAzureFunctionsConstants.DefaultGraphQLRoute);

    public static HttpContext NewGraphQLHttpContext(string query)
    {
        var httpContext = new DefaultHttpContext();

        var request = httpContext.Request;
        request.Method = HttpMethods.Post;
        request.Scheme = DefaultAzFuncGraphQLUri.Scheme;
        request.Host = new HostString(DefaultAzFuncGraphQLUri.Host, DefaultAzFuncGraphQLUri.Port);
        request.Path = new PathString(DefaultAzFuncGraphQLUri.AbsolutePath);
        request.QueryString = new QueryString(DefaultAzFuncGraphQLUri.Query);
        request.Body = new MemoryStream(Encoding.UTF8.GetBytes(CreateRequestBody(query)));
        request.ContentType = TestConstants.DefaultJsonContentType;

        httpContext.Response.Body = new MemoryStream();

        return httpContext;
    }

    public static HttpContext NewNitroHttpContext()
    {
        var uri = new Uri(IO.Path.Combine(DefaultAzFuncGraphQLUri.ToString(), "index.html"));

        var httpContext = new DefaultHttpContext();

        var request = httpContext.Request;
        request.Method = HttpMethods.Get;
        request.Scheme = uri.Scheme;
        request.Host = new HostString(uri.Host, uri.Port);
        request.Path = new PathString(uri.AbsolutePath);
        request.QueryString = new QueryString(uri.Query);

        // Ensure we accept Text/Html for Nitro requests...
        httpContext.Request.Headers[HeaderNames.Accept] = TestConstants.DefaultNitroContentType;

        httpContext.Response.Body = new MemoryStream();

        return httpContext;
    }

    public static string CreateRequestBody(string query)
        => JObject.FromObject(new { query = query, }).ToString(Formatting.Indented);
}
