using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore;

public sealed class HttpContentNegotiationMiddlewareTests
{
    [Theory]
    [InlineData("/graphql", "GET", "/graphql")]
    [InlineData("/graphql", "HEAD", "/graphql")]
    [InlineData("/graphql", "POST", "/graphql")]
    [InlineData("/graphql", "QUERY", "/graphql")]
    [InlineData("/graphql", "GET", "/graphql/")]
    [InlineData("/graphql", "GET", "/GraphQL")]
    [InlineData("", "GET", "/")]
    [InlineData(null, "GET", "/anything")]
    [InlineData(null, "POST", "/")]
    public async Task InvokeAsync_Should_AddAcceptToVary_When_EndpointSelectsResponseByAccept(
        string? endpointPath,
        string method,
        string requestPath)
    {
        // arrange
        var context = CreateContext(method, requestPath);
        var nextRan = false;
        var middleware = CreateMiddleware(endpointPath, () => nextRan = true);

        // act
        await middleware.InvokeAsync(context);

        // assert
        Assert.Equal(new StringValues("Accept"), context.Response.Headers.Vary);
        Assert.True(nextRan);
    }

    [Theory]
    [InlineData("/graphql", "OPTIONS", "/graphql")]
    [InlineData("/graphql", "PUT", "/graphql")]
    [InlineData("/graphql", "DELETE", "/graphql")]
    [InlineData("/graphql", "GET", "/graphql/nitro-config.json")]
    [InlineData("/graphql", "GET", "/graphql-x")]
    [InlineData("/graphql", "GET", "/other")]
    [InlineData(null, "OPTIONS", "/anything")]
    public async Task InvokeAsync_Should_LeaveVaryEmpty_When_EndpointDoesNotSelectResponseByAccept(
        string? endpointPath,
        string method,
        string requestPath)
    {
        // arrange
        var context = CreateContext(method, requestPath);
        var nextRan = false;
        var middleware = CreateMiddleware(endpointPath, () => nextRan = true);

        // act
        await middleware.InvokeAsync(context);

        // assert
        Assert.Equal(StringValues.Empty, context.Response.Headers.Vary);
        Assert.True(nextRan);
    }

    [Fact]
    public async Task InvokeAsync_Should_KeepExistingVaryValues_When_VaryIsAlreadySet()
    {
        // arrange
        var context = CreateContext("GET", "/graphql");
        context.Response.Headers.Vary = "Origin";
        var middleware = CreateMiddleware("/graphql", () => { });

        // act
        await middleware.InvokeAsync(context);

        // assert
        Assert.Equal(new StringValues(["Origin", "Accept"]), context.Response.Headers.Vary);
    }

    private static DefaultHttpContext CreateContext(string method, string requestPath)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = requestPath;
        return context;
    }

    private static HttpContentNegotiationMiddleware CreateMiddleware(
        string? endpointPath,
        Action onNext)
        => new(
            _ =>
            {
                onNext();
                return Task.CompletedTask;
            },
            endpointPath is null ? (PathString?)null : new PathString(endpointPath));
}
