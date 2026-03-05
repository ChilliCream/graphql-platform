using System.Text;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mcp.Serve.Schema;

public sealed class ApiResolverTests
{
    private static string MakeBase64ApiId(string rawId)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes("Api:" + rawId));

    [Fact]
    public void Resolve_ExplicitParam_Wins_Over_Context()
    {
        var context = new NitroMcpContext("context-api-id", "production");
        var resolver = new ApiResolver(context);
        var explicitId = MakeBase64ApiId("explicit-123");

        var result = resolver.Resolve(explicitId);

        Assert.True(result.IsSuccess);
        Assert.Equal(explicitId, result.ApiId);
    }

    [Fact]
    public void Resolve_FallsBackToContext_When_NoExplicitParameter()
    {
        var contextId = MakeBase64ApiId("context-456");
        var context = new NitroMcpContext(contextId, "production");
        var resolver = new ApiResolver(context);

        var result = resolver.Resolve(null);

        Assert.True(result.IsSuccess);
        Assert.Equal(contextId, result.ApiId);
    }

    [Fact]
    public void Resolve_Returns_Context_ApiId_When_EmptyParam()
    {
        var contextId = MakeBase64ApiId("ctx-789");
        var context = new NitroMcpContext(contextId, "staging");
        var resolver = new ApiResolver(context);

        var result = resolver.Resolve("");

        Assert.True(result.IsSuccess);
        Assert.Equal(contextId, result.ApiId);
    }

    [Fact]
    public void Resolve_Returns_Error_When_No_ApiId_Available()
    {
        var context = new NitroMcpContext("", "production");
        var resolver = new ApiResolver(context);

        var result = resolver.Resolve(null);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Cannot resolve API", result.ErrorMessage);
    }

    [Fact]
    public void Resolve_Accepts_Any_NonEmpty_ApiId()
    {
        var context = new NitroMcpContext("", "production");
        var resolver = new ApiResolver(context);

        var result = resolver.Resolve("plain-api-id");

        Assert.True(result.IsSuccess);
        Assert.Equal("plain-api-id", result.ApiId);
        Assert.Equal("plain-api-id", result.ApiName);
    }

    [Fact]
    public void Resolve_Uses_ApiId_As_Name_When_No_ProjectContext()
    {
        var context = new NitroMcpContext("", "production");
        var resolver = new ApiResolver(context);
        var apiId = MakeBase64ApiId("my-cool-api");

        var result = resolver.Resolve(apiId);

        Assert.True(result.IsSuccess);
        Assert.Equal(apiId, result.ApiName);
    }

    [Fact]
    public void Resolve_ExplicitParam_TakesPrecedence_Over_Context()
    {
        var contextId = MakeBase64ApiId("context-api");
        var explicitId = MakeBase64ApiId("explicit-api");
        var context = new NitroMcpContext(contextId, "production");
        var resolver = new ApiResolver(context);

        var result = resolver.Resolve(explicitId);

        Assert.True(result.IsSuccess);
        Assert.Equal(explicitId, result.ApiId);
    }
}
