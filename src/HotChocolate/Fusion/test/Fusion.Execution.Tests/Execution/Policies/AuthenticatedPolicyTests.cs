using System.Security.Claims;
using HotChocolate.Features;

namespace HotChocolate.Fusion.Execution;

public sealed class AuthenticatedPolicyTests
{
    [Fact]
    public void Name_Should_BeFusionAuthenticated()
    {
        // arrange
        var policy = new AuthenticatedPolicy();

        // act & assert
        Assert.Equal("fusion.authenticated", policy.Name);
    }

    [Fact]
    public void Requirements_Should_BeRequestCacheable()
    {
        // arrange
        var policy = new AuthenticatedPolicy();

        // act & assert
        Assert.True(policy.Requirements.IsRequestCacheable);
    }

    [Fact]
    public async Task EvaluateAsync_Should_Allow_When_UserIsAuthenticated()
    {
        // arrange
        var policy = new AuthenticatedPolicy();
        var context = new PolicyContext(FeatureCollection.Empty);
        context.ResetForRequest(new ClaimsPrincipal(new ClaimsIdentity([], "test")));

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.False(context.IsDenied(0));
    }

    [Fact]
    public async Task EvaluateAsync_Should_Deny_When_UserIsNotAuthenticated()
    {
        // arrange
        var policy = new AuthenticatedPolicy();
        var context = new PolicyContext(FeatureCollection.Empty);
        context.ResetForRequest(new ClaimsPrincipal());

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.True(context.IsDenied(0));
    }
}
