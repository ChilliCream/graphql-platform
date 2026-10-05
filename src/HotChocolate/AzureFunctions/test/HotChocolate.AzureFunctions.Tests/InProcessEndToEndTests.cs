using HotChocolate.AzureFunctions.Tests.Helpers;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace HotChocolate.AzureFunctions.Tests;

public class InProcessEndToEndTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("*/*")]
    [InlineData("application/graphql-response+json, application/json;q=0.9")]
    [InlineData("application/graphql-response+json, text/html;q=0.1")]
    [InlineData("text/html;q=0")]
    public async Task Get_Should_ReturnNotFound_When_AcceptDoesNotPreferHtml(string? accept)
    {
        // arrange
        var hostBuilder = new MockInProcessFunctionsHostBuilder();
        hostBuilder.Services.AddHttpContextAccessor();
        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker"));
        var requestExecutor = hostBuilder
            .BuildServiceProvider()
            .GetRequiredService<IGraphQLRequestExecutor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = TestHttpContextHelper.DefaultAzFuncGraphQLUri.AbsolutePath;
        httpContext.Response.Body = new MemoryStream();

        if (accept is not null)
        {
            httpContext.Request.Headers.Accept = accept;
        }

        // act
        await requestExecutor.ExecuteAsync(httpContext.Request);

        // assert
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Equal("Accept", httpContext.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task Get_Should_RedirectToTool_When_AcceptPrefersHtml()
    {
        // arrange
        var hostBuilder = new MockInProcessFunctionsHostBuilder();
        hostBuilder.Services.AddHttpContextAccessor();
        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker"));
        var requestExecutor = hostBuilder
            .BuildServiceProvider()
            .GetRequiredService<IGraphQLRequestExecutor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = TestHttpContextHelper.DefaultAzFuncGraphQLUri.AbsolutePath;
        httpContext.Request.Headers.Accept =
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        httpContext.Response.Body = new MemoryStream();

        // act
        await requestExecutor.ExecuteAsync(httpContext.Request);

        // assert
        Assert.Equal(StatusCodes.Status301MovedPermanently, httpContext.Response.StatusCode);
        Assert.Equal("Accept", httpContext.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task AzFuncInProcess_EndToEndTestAsync()
    {
        var hostBuilder = new MockInProcessFunctionsHostBuilder();

        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker"));

        var serviceProvider = hostBuilder.BuildServiceProvider();

        // The executor should resolve without error as a Required service...
        var requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        var httpContext = TestHttpContextHelper.NewGraphQLHttpContext(
            @"query {
                person
            }");

        // Execute Query Test for end-to-end validation...
        await requestExecutor.ExecuteAsync(httpContext.Request);

        // Read, Parse & Validate the response...
        var resultContent = await httpContext.ReadResponseContentAsync();
        Assert.False(string.IsNullOrWhiteSpace(resultContent));

        dynamic json = JObject.Parse(resultContent);
        Assert.Null(json.errors);
        Assert.Equal("Luke Skywalker", json.data.person.ToString());
    }

    [Fact]
    public async Task Post_Should_ReturnVaryAccept_When_QueryIsExecuted()
    {
        // arrange
        var hostBuilder = new MockInProcessFunctionsHostBuilder();
        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker"));
        var requestExecutor = hostBuilder
            .BuildServiceProvider()
            .GetRequiredService<IGraphQLRequestExecutor>();
        var httpContext = TestHttpContextHelper.NewGraphQLHttpContext("{ person }");

        // act
        await requestExecutor.ExecuteAsync(httpContext.Request);

        // assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("Accept", httpContext.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task Get_Should_ReturnVaryAccept_When_QueryIsExecuted()
    {
        // arrange
        var hostBuilder = new MockInProcessFunctionsHostBuilder();
        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(d => d.Name("Query").Field("person").Resolve("Luke Skywalker"));
        var requestExecutor = hostBuilder
            .BuildServiceProvider()
            .GetRequiredService<IGraphQLRequestExecutor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = TestHttpContextHelper.DefaultAzFuncGraphQLUri.AbsolutePath;
        httpContext.Request.QueryString = new QueryString("?query=%7B%20person%20%7D");
        httpContext.Response.Body = new MemoryStream();

        // act
        await requestExecutor.ExecuteAsync(httpContext.Request);

        // assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("Accept", httpContext.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task AzFuncInProcess_NitroTestAsync()
    {
        var hostBuilder = new MockInProcessFunctionsHostBuilder();

        hostBuilder.Services.AddHttpContextAccessor();

        hostBuilder
            .AddGraphQLFunction()
            .AddQueryType(
                d => d.Name("Query")
                    .Field("NitroTest")
                    .Resolve("This is a test for Nitro File Serving..."));

        var serviceProvider = hostBuilder.BuildServiceProvider();

        // The executor should resolve without error as a Required service...
        var requestExecutor = serviceProvider.GetRequiredService<IGraphQLRequestExecutor>();

        var httpContext = TestHttpContextHelper.NewNitroHttpContext();

        // Execute Query Test for end-to-end validation...
        await requestExecutor.ExecuteAsync(httpContext.Request);

        // Read, Parse & Validate the response...
        var resultContent = await httpContext.ReadResponseContentAsync();
        Assert.NotNull(resultContent);
        Assert.False(string.IsNullOrWhiteSpace(resultContent));
        Assert.True(resultContent.Contains("<html") && resultContent.Contains("</html>"));
    }
}
