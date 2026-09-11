using System.Security.Claims;
using HotChocolate.Features;

namespace HotChocolate.Fusion.Execution;

public sealed class ScopePolicyTests
{
    [Fact]
    public void Name_Should_BeScopePrefixedName()
    {
        // arrange
        var policy = new ScopePolicy("read:users", ["scope"]);

        // act & assert
        Assert.Equal("fusion.scope:read:users", policy.Name);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Allow_When_DefaultScopeClaimContainsScope()
    {
        // arrange
        var policy = new ScopePolicy("read:users", ["scope", "scp"]);
        var context = new PolicyContext(FeatureCollection.Empty);
        context.ResetForRequest(Principal(new Claim("scope", "read:users write:users")));

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.False(context.IsDenied(0));
    }

    [Fact]
    public async Task EvaluateAsync_Should_Allow_When_ScpClaimContainsScope()
    {
        // arrange
        var policy = new ScopePolicy("admin", ["scope", "scp"]);
        var context = new PolicyContext(FeatureCollection.Empty);
        context.ResetForRequest(Principal(new Claim("scp", "admin")));

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.False(context.IsDenied(0));
    }

    [Fact]
    public async Task EvaluateAsync_Should_Allow_When_ScopeIsInARepeatedClaim()
    {
        // arrange
        var policy = new ScopePolicy("read:orders", ["scope"]);
        var context = new PolicyContext(FeatureCollection.Empty);
        context.ResetForRequest(
            Principal(new Claim("scope", "read:users"), new Claim("scope", "read:orders")));

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.False(context.IsDenied(0));
    }

    [Fact]
    public async Task EvaluateAsync_Should_Deny_When_ScopeIsMissing()
    {
        // arrange
        var policy = new ScopePolicy("admin", ["scope", "scp"]);
        var context = new PolicyContext(FeatureCollection.Empty);
        context.ResetForRequest(Principal(new Claim("scope", "read:users")));

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.True(context.IsDenied(0));
    }

    [Fact]
    public async Task EvaluateAsync_Should_Deny_When_ScopeDiffersOnlyByCase()
    {
        // arrange
        var policy = new ScopePolicy("Admin", ["scope"]);
        var context = new PolicyContext(FeatureCollection.Empty);
        context.ResetForRequest(Principal(new Claim("scope", "admin")));

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.True(context.IsDenied(0));
    }

    [Fact]
    public async Task EvaluateAsync_Should_Deny_When_ClaimTypeIsNotConfigured()
    {
        // arrange
        var policy = new ScopePolicy("admin", ["scope"]);
        var context = new PolicyContext(FeatureCollection.Empty);
        context.ResetForRequest(Principal(new Claim("permissions", "admin")));

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.True(context.IsDenied(0));
    }

    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "test"));
}
