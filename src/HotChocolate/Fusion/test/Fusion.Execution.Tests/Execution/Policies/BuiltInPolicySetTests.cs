namespace HotChocolate.Fusion.Execution;

public sealed class BuiltInPolicySetTests
{
    [Fact]
    public void Create_Should_IncludeAuthenticatedPolicy_When_ItsNameIsReferenced()
    {
        // arrange
        var referencedNames = new[] { BuiltInPolicyNames.Authenticated };

        // act
        var policies = BuiltInPolicySet.Create(referencedNames, BuiltInPolicySet.DefaultScopeClaimTypes);

        // assert
        var policy = Assert.Single(policies);
        Assert.IsType<AuthenticatedPolicy>(policy);
    }

    [Fact]
    public void Create_Should_IncludeScopePolicy_When_AScopeNameIsReferenced()
    {
        // arrange
        var referencedNames = new[] { "fusion.scope:read:users" };

        // act
        var policies = BuiltInPolicySet.Create(referencedNames, BuiltInPolicySet.DefaultScopeClaimTypes);

        // assert
        var policy = Assert.Single(policies);
        Assert.Equal("fusion.scope:read:users", policy.Name);
    }

    [Fact]
    public void Create_Should_ReturnEmpty_When_NoBuiltInNameIsReferenced()
    {
        // act
        var policies = BuiltInPolicySet.Create([], BuiltInPolicySet.DefaultScopeClaimTypes);

        // assert
        Assert.Empty(policies);
    }
}
