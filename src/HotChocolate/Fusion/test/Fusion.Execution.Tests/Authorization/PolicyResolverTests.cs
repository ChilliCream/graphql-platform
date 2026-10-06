using HotChocolate.Types;

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
        var policy = resolver.Resolve("p", DirectiveNames.Policy.Name);

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
        var policy = resolver.Resolve("p", DirectiveNames.Policy.Name);

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
        var policy = resolver.Resolve("p", DirectiveNames.Policy.Name);

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
        var authenticated = resolver.Resolve(string.Empty, DirectiveNames.Authenticated.Name);
        var scopes = resolver.Resolve(string.Empty, DirectiveNames.RequiresScopes.Name);

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
        var policy = provider.GetPolicy("authenticated", DirectiveNames.Policy.Name);

        // assert
        Assert.Null(policy);
    }

    [Fact]
    public void Get_Should_OrderAuthenticatedBeforeScopesBeforePolicies_When_Compared()
    {
        // arrange
        string[] names =
        [
            DirectiveNames.Policy.Name,
            DirectiveNames.RequiresScopes.Name,
            DirectiveNames.Authenticated.Name
        ];

        // act
        var ordered = names.OrderBy(PolicyEvaluationOrder.Get).ToArray();

        // assert
        Assert.Equal(
            [
                DirectiveNames.Authenticated.Name,
                DirectiveNames.RequiresScopes.Name,
                DirectiveNames.Policy.Name
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
