using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class AuthorizationPolicyCollectionTests
{
    [Fact]
    public void Get_Should_ReturnPolicy_When_NameMatchesExactly()
    {
        var policy = new TestAuthorizationPolicy("CanReadSecret");
        var policies = new AuthorizationPolicyCollection([policy]);

        Assert.Same(policy, policies.Get("CanReadSecret"));
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_NameDiffersByCase()
    {
        var policies = new AuthorizationPolicyCollection(
            [new TestAuthorizationPolicy("CanReadSecret")]);

        Assert.False(policies.TryGet("canReadSecret", out _));
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_NameIsEmpty()
    {
        var policies = new AuthorizationPolicyCollection(
            [new TestAuthorizationPolicy("CanReadSecret")]);

        Assert.False(policies.TryGet(string.Empty, out _));
    }

    [Fact]
    public void Constructor_Should_Throw_When_PolicyNameIsDuplicated()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new AuthorizationPolicyCollection(
                [
                    new TestAuthorizationPolicy("CanReadSecret"),
                    new TestAuthorizationPolicy("CanReadSecret")
                ]));

        Assert.Equal(
            "Authorization policy 'CanReadSecret' is registered more than once.",
            exception.Message);
    }

    [Fact]
    public void Constructor_Should_Throw_When_PolicyNameIsEmpty()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new AuthorizationPolicyCollection(
                [new TestAuthorizationPolicy(string.Empty)]));

        Assert.Equal("An authorization policy must have a name.", exception.Message);
    }

    [Fact]
    public void CreateSchema_Should_Throw_When_PolicyNameDiffersByCase()
    {
        using var services = new ServiceCollection()
            .AddSingleton<IAuthorizationPolicyProvider>(
                _ => new TestAuthorizationPolicyProvider(
                    new TestAuthorizationPolicy("CanReadSecret")))
            .BuildServiceProvider();

        var exception = Assert.Throws<KeyNotFoundException>(
            () => FusionSchemaDefinition.Create(
                CreateSchemaDocument("canReadSecret"),
                services));

        Assert.Equal(
            "Authorization policy 'canReadSecret' was not found.",
            exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeProviderAndPolicyOnce_When_SchemaIsDisposed()
    {
        var policy = new DisposablePolicy();
        var provider = new TestAuthorizationPolicyProvider(policy);
        var services = new ServiceCollection()
            .AddSingleton<IAuthorizationPolicyProvider>(_ => provider)
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            CreateSchemaDocument(policy.Name),
            services);

        Assert.Same(policy, schema.Policies.Get(policy.Name));
        Assert.Same(policy, schema.Policies.Get(policy.Name));
        Assert.Equal(1, provider.CreateCalls);

        await schema.DisposeAsync();
        await schema.DisposeAsync();

        Assert.True(provider.IsDisposed);
        Assert.Equal(1, policy.DisposeCalls);
    }

    private static DocumentNode CreateSchemaDocument(string policyName)
        => Utf8GraphQLParser.Parse(
            $$"""
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "{{policyName}}")
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """);

    private sealed class DisposablePolicy : IAuthorizationPolicy, IDisposable
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public int DisposeCalls { get; private set; }

        public ValueTask EvaluateAsync(
            IAuthorizationContext context,
            EntityData entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public void Dispose() => DisposeCalls++;
    }
}
