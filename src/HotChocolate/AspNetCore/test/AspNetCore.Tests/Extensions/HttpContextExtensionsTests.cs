using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Extensions;

public sealed class HttpContextExtensionsTests
{
    private const string BrowserAccept =
        "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

    [Theory]
    [InlineData("GET", "/graphql", "/graphql")]
    [InlineData("HEAD", "/graphql", "/graphql")]
    [InlineData("GET", "/graphql/", "/graphql")]
    [InlineData("GET", "/GraphQL", "/graphql")]
    [InlineData("GET", "/", "")]
    public void MayReachNitroApp_Should_ReturnTrue_When_AcceptPrefersHtml(
        string method,
        string requestPath,
        string endpointPath)
    {
        // arrange
        var context = CreateContext(method, requestPath, BrowserAccept);

        // act
        var mayReachNitroApp = context.MayReachNitroApp(endpointPath);

        // assert
        Assert.True(mayReachNitroApp);
    }

    [Theory]
    [InlineData("GET", "/graphql", "/graphql", null)]
    [InlineData("GET", "/graphql", "/graphql", "*/*")]
    [InlineData("HEAD", "/graphql/", "/graphql", "*/*")]
    [InlineData("GET", "/", "", "*/*")]
    public void MayReachNitroApp_Should_ReturnFalse_When_AcceptDoesNotPreferHtml(
        string method,
        string requestPath,
        string endpointPath,
        string? accept)
    {
        // arrange
        var context = CreateContext(method, requestPath, accept);

        // act
        var mayReachNitroApp = context.MayReachNitroApp(endpointPath);

        // assert
        Assert.False(mayReachNitroApp);
    }

    [Fact]
    public void MayReachNitroApp_Should_ReturnFalse_When_AcceptIsUnparsable()
    {
        // arrange
        // a quality above 1 makes the Accept header unparsable
        var context = CreateContext("GET", "/graphql", "text/html;q=2");

        // act
        var mayReachNitroApp = context.MayReachNitroApp("/graphql");

        // assert
        Assert.False(mayReachNitroApp);
    }

    [Theory]
    [InlineData("POST", "/graphql")]
    [InlineData("OPTIONS", "/graphql")]
    [InlineData("GET", "/graphql/nitro-config.json")]
    [InlineData("GET", "/other")]
    public void MayReachNitroApp_Should_ReturnTrue_When_RequestIsNotAGetOrHeadOnTheEndpointPath(
        string method,
        string requestPath)
    {
        // arrange
        var context = CreateContext(method, requestPath, "*/*");

        // act
        var mayReachNitroApp = context.MayReachNitroApp("/graphql");

        // assert
        Assert.True(mayReachNitroApp);
    }

    private static DefaultHttpContext CreateContext(
        string method,
        string requestPath,
        string? accept)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = requestPath;

        if (accept is not null)
        {
            context.Request.Headers.Accept = accept;
        }

        return context;
    }
}
