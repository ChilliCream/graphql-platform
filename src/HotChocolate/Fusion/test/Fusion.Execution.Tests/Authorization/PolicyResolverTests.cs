namespace HotChocolate.Fusion.Authorization;

public class PolicyResolverTests
{
    [Fact]
    public void Resolve_Should_ReturnPolicyOfFirstProvider_When_SeveralProvidersKnowTheName()
    {
        // arrange
        var first = new StubPolicy();
        var second = new StubPolicy();
        var resolver = new PolicyResolver(
            new BuiltInPolicyProvider(),
            [new StubProvider(first), new StubProvider(second)]);

        // act
        var policy = resolver.Resolve("p", PolicyDirectiveNames.Policy);

        // assert
        Assert.Same(first, policy);
    }

    [Fact]
    public void Resolve_Should_FallThroughToNextProvider_When_FirstProviderReturnsNull()
    {
        // arrange
        var second = new StubPolicy();
        var resolver = new PolicyResolver(
            new BuiltInPolicyProvider(),
            [new StubProvider(null), new StubProvider(second)]);

        // act
        var policy = resolver.Resolve("p", PolicyDirectiveNames.Policy);

        // assert
        Assert.Same(second, policy);
    }

    [Fact]
    public void Resolve_Should_ReturnNull_When_NoProviderKnowsThePolicy()
    {
        // arrange
        var resolver = new PolicyResolver(
            new BuiltInPolicyProvider(),
            [new StubProvider(null)]);

        // act
        var policy = resolver.Resolve("p", PolicyDirectiveNames.Policy);

        // assert
        Assert.Null(policy);
    }

    [Fact]
    public void Resolve_Should_ReturnBuiltInPolicy_When_UserProviderAnswersBuiltInDirective()
    {
        // arrange
        var resolver = new PolicyResolver(
            new BuiltInPolicyProvider(),
            [new StubProvider(new StubPolicy())]);

        // act
        var authenticated = resolver.Resolve(string.Empty, PolicyDirectiveNames.Authenticated);
        var scopes = resolver.Resolve(string.Empty, PolicyDirectiveNames.RequiresScopes);

        // assert
        Assert.Same(AuthenticatedPolicy.Instance, authenticated);
        Assert.IsType<RequiresScopesPolicy>(scopes);
    }

    [Fact]
    public void GetPolicy_Should_ReturnNull_When_DirectiveIsPolicy()
    {
        // arrange
        var provider = new BuiltInPolicyProvider();

        // act
        var policy = provider.GetPolicy("authenticated", PolicyDirectiveNames.Policy);

        // assert
        Assert.Null(policy);
    }

    [Fact]
    public void GetEvaluationOrder_Should_OrderAuthenticatedBeforeScopesBeforePolicies_When_Compared()
    {
        // arrange
        string[] names =
        [
            PolicyDirectiveNames.Policy,
            PolicyDirectiveNames.RequiresScopes,
            PolicyDirectiveNames.Authenticated
        ];

        // act
        var ordered = names.OrderBy(PolicyDirectiveNames.GetEvaluationOrder).ToArray();

        // assert
        Assert.Equal(
            [
                PolicyDirectiveNames.Authenticated,
                PolicyDirectiveNames.RequiresScopes,
                PolicyDirectiveNames.Policy
            ],
            ordered);
    }

    private sealed class StubPolicy : IPolicy
    {
        public ValueTask EvaluateAsync(
            PolicyEvaluationContext context,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class StubProvider(IPolicy? policy) : IPolicyProvider
    {
        public IPolicy? GetPolicy(string policyName, string directiveName) => policy;
    }
}
