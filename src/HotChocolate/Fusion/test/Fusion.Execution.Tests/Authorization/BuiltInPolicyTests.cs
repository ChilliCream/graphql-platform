using System.Security.Claims;
using static HotChocolate.Fusion.Authorization.PolicyTestHelper;

namespace HotChocolate.Fusion.Authorization;

public class BuiltInPolicyTests
{
    [Fact]
    public async Task AuthenticatedPolicy_Should_AllowAllEntries_When_PrincipalIsAuthenticated()
    {
        // arrange
        var selections = CreateSelections();
        var policy = AuthenticatedPolicy.Instance;
        var context = CreateContext(
            Authenticated(),
            CreateEntry(selections[1], policy, PolicyDirectiveNames.Authenticated, null),
            CreateEntry(selections[2], policy, PolicyDirectiveNames.Authenticated, null));

        // act
        await policy.EvaluateAsync(context, CancellationToken.None);

        // assert
        Assert.Equal([PolicyOutcome.Allowed, PolicyOutcome.Allowed], Outcomes(context));
    }

    [Fact]
    public async Task AuthenticatedPolicy_Should_DenyAllEntries_When_PrincipalIsAnonymous()
    {
        // arrange
        var selections = CreateSelections();
        var policy = AuthenticatedPolicy.Instance;
        var context = CreateContext(
            Anonymous(),
            CreateEntry(selections[1], policy, PolicyDirectiveNames.Authenticated, null),
            CreateEntry(selections[2], policy, PolicyDirectiveNames.Authenticated, null));

        // act
        await policy.EvaluateAsync(context, CancellationToken.None);

        // assert
        Assert.Equal([PolicyOutcome.Denied, PolicyOutcome.Denied], Outcomes(context));
    }

    [Fact]
    public async Task RequiresScopesPolicy_Should_DenyEvenWithScopeClaim_When_PrincipalIsNotAuthenticated()
    {
        // arrange
        var selections = CreateSelections();
        var policy = new RequiresScopesPolicy();
        var anonymousWithScope = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("scope", "read")]));
        var context = CreateContext(
            anonymousWithScope,
            CreateEntry(
                selections[1],
                policy,
                PolicyDirectiveNames.RequiresScopes,
                null,
                [["read"]]));

        // act
        await policy.EvaluateAsync(context, CancellationToken.None);

        // assert
        Assert.Equal([PolicyOutcome.Denied], Outcomes(context));
    }

    [Fact]
    public async Task RequiresScopesPolicy_Should_CombineGroupsAsOrOfAnd_When_ScopesAreEvaluated()
    {
        // arrange
        var selections = CreateSelections();
        var policy = new RequiresScopesPolicy();
        var context = CreateContext(
            Authenticated(new Claim("scope", "read write"), new Claim("scope", "admin")),
            CreateEntry(selections[0], policy, PolicyDirectiveNames.RequiresScopes, null, [["read", "write"]]),
            CreateEntry(selections[1], policy, PolicyDirectiveNames.RequiresScopes, null, [["read", "delete"], ["admin"]]),
            CreateEntry(selections[2], policy, PolicyDirectiveNames.RequiresScopes, null, [["read", "delete"]]),
            CreateEntry(selections[2], policy, PolicyDirectiveNames.RequiresScopes, null, []));

        // act
        await policy.EvaluateAsync(context, CancellationToken.None);

        // assert
        Assert.Equal(
            [PolicyOutcome.Allowed, PolicyOutcome.Allowed, PolicyOutcome.Denied, PolicyOutcome.Denied],
            Outcomes(context));
    }

    [Fact]
    public async Task RequiresScopesPolicy_Should_ReadConfiguredClaimType_When_ClaimTypeIsGiven()
    {
        // arrange
        var selections = CreateSelections();
        var policy = new RequiresScopesPolicy("scp");
        var context = CreateContext(
            Authenticated(new Claim("scope", "read"), new Claim("scp", "write")),
            CreateEntry(selections[1], policy, PolicyDirectiveNames.RequiresScopes, null, [["write"]]),
            CreateEntry(selections[2], policy, PolicyDirectiveNames.RequiresScopes, null, [["read"]]));

        // act
        await policy.EvaluateAsync(context, CancellationToken.None);

        // assert
        Assert.Equal([PolicyOutcome.Allowed, PolicyOutcome.Denied], Outcomes(context));
    }

    [Fact]
    public async Task RequiresScopesPolicy_Should_DenyWithEmptyScopes_When_DescriptorScopesAreDefault()
    {
        // arrange
        var selections = CreateSelections();
        var policy = new RequiresScopesPolicy();
        var entry = CreateEntry(selections[1], policy, PolicyDirectiveNames.RequiresScopes, null);
        var context = CreateContext(Authenticated(new Claim("scope", "read")), entry);

        // act
        await policy.EvaluateAsync(context, CancellationToken.None);

        // assert
        Assert.False(entry.Descriptor.Scopes.IsDefault);
        Assert.Empty(entry.Descriptor.Scopes);
        Assert.Equal([PolicyOutcome.Denied], Outcomes(context));
    }
}
