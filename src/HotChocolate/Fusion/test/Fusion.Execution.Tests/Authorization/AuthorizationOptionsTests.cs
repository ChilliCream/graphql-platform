using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Authorization.PolicyTestHelper;

namespace HotChocolate.Fusion.Authorization;

public class AuthorizationOptionsTests : FusionTestBase
{
    [Fact]
    public async Task ModifyAuthorizationOptions_Should_ExposeDefaults_When_NoModifierIsRegistered()
    {
        // arrange
        var executor = await CreateExecutorAsync(builder => builder);

        // act
        var options = executor.Schema.Services.GetRequiredService<FusionAuthorizationOptions>();

        // assert
        Describe(options).MatchInlineSnapshot(
            """
            DenyHandling: Null
            RejectRequestOn: Off
            Schemes: unset
            SchemeChallenges: none
            ScopeClaimName: scope
            ScopeClaimFormat: SpaceSeparated
            EnableAttribution: False
            DisableAuthorizationValidation: False
            """);
    }

    [Fact]
    public async Task ModifyAuthorizationOptions_Should_ApplyEveryModifier_When_ExecutorIsCreated()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            builder => builder
                .ModifyAuthorizationOptions(
                    o =>
                    {
                        o.DenyHandling = DenyHandling.Error;
                        o.RejectRequestOn = RejectRequestOn.OnUnauthenticated;
                        o.Schemes = ImmutableArray.Create("Bearer", "Cookie");
                        o.SchemeChallenges = ImmutableDictionary<string, string>.Empty.Add("Cookie", "Cookie");
                    })
                .ModifyAuthorizationOptions(
                    o =>
                    {
                        o.RejectRequestOn = RejectRequestOn.OnUnauthorized;
                        o.ScopeClaimName = "scp";
                        o.ScopeClaimFormat = ScopeClaimFormat.Array;
                        o.EnableAttribution = true;
                        o.DisableAuthorizationValidation = true;
                    }));

        // act
        var options = executor.Schema.Services.GetRequiredService<FusionAuthorizationOptions>();

        // assert
        Describe(options).MatchInlineSnapshot(
            """
            DenyHandling: Error
            RejectRequestOn: OnUnauthorized
            Schemes: Bearer,Cookie
            SchemeChallenges: Cookie=Cookie
            ScopeClaimName: scp
            ScopeClaimFormat: Array
            EnableAttribution: True
            DisableAuthorizationValidation: True
            """);
    }

    [Fact]
    public async Task Options_Should_BeReadOnly_When_ExecutorIsCreated()
    {
        // arrange
        var executor = await CreateExecutorAsync(builder => builder);
        var options = executor.Schema.Services.GetRequiredService<FusionAuthorizationOptions>();

        // act
        Action act = () => options.DenyHandling = DenyHandling.Error;

        // assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("The authorization options are read-only.", exception.Message);
    }

    [Fact]
    public void Schemes_Should_BeUnset_When_DefaultArrayIsAssigned()
    {
        // arrange
        var options = new FusionAuthorizationOptions { Schemes = ImmutableArray.Create("Bearer") };

        // act
        options.Schemes = default(ImmutableArray<string>);

        // assert
        Assert.Null(options.Schemes);
    }

    [Fact]
    public void SchemeChallenges_Should_Throw_When_ValueIsNull()
    {
        // arrange
        var options = new FusionAuthorizationOptions();

        // act
        var act = () => options.SchemeChallenges = null!;

        // assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SchemeChallenges_Should_Throw_When_ChallengeIsEmpty(string? challenge)
    {
        // arrange
        var options = new FusionAuthorizationOptions();
        var challenges = ImmutableDictionary<string, string>.Empty.Add("MyJwt", challenge!);

        // act
        var act = () => options.SchemeChallenges = challenges;

        // assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Equal(
            "The WWW-Authenticate challenge of the authentication scheme 'MyJwt' must not be null, "
            + "empty or whitespace. (Parameter 'value')",
            exception.Message);
    }

    [Fact]
    public void ScopeClaimName_Should_Throw_When_ValueIsEmpty()
    {
        // arrange
        var options = new FusionAuthorizationOptions();

        // act
        var act = () => options.ScopeClaimName = string.Empty;

        // assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public async Task PolicyResolver_Should_ReadConfiguredScopeClaim_When_ScopeOptionsAreModified()
    {
        // arrange
        var executor = await CreateExecutorAsync(
            builder => builder.ModifyAuthorizationOptions(
                o =>
                {
                    o.ScopeClaimName = "scp";
                    o.ScopeClaimFormat = ScopeClaimFormat.Array;
                }));
        var resolver = executor.Schema.Services.GetRequiredService<IPolicyResolver>();
        var policy = resolver.Resolve(string.Empty, DirectiveNames.RequiresScopes.Name)!;
        var selections = CreateSelections();
        var context = CreateContext(
            Authenticated(new Claim("scope", "read"), new Claim("scp", "read"), new Claim("scp", "write")),
            CreateEntry(selections[1], policy, DirectiveNames.RequiresScopes.Name, null, [["read", "write"]]),
            CreateEntry(selections[2], policy, DirectiveNames.RequiresScopes.Name, null, [["read write"]]));

        // act
        await policy.EvaluateAsync(context, CancellationToken.None);

        // assert
        Assert.Equal([PolicyOutcome.Allowed, PolicyOutcome.Denied], Outcomes(context));
    }

    [Fact]
    public async Task PolicyResolver_Should_ReadScopeClaim_When_ScopeOptionsAreNotModified()
    {
        // arrange
        var executor = await CreateExecutorAsync(builder => builder);
        var resolver = executor.Schema.Services.GetRequiredService<IPolicyResolver>();
        var policy = resolver.Resolve(string.Empty, DirectiveNames.RequiresScopes.Name)!;
        var selections = CreateSelections();
        var context = CreateContext(
            Authenticated(new Claim("scope", "read write")),
            CreateEntry(selections[1], policy, DirectiveNames.RequiresScopes.Name, null, [["read", "write"]]));

        // act
        await policy.EvaluateAsync(context, CancellationToken.None);

        // assert
        Assert.Equal([PolicyOutcome.Allowed], Outcomes(context));
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        Func<IFusionGatewayBuilder, IFusionGatewayBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(ComposeSchemaDocument("type Query { field: String! }"));
        configure(builder);

        return await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }

    private static string Describe(FusionAuthorizationOptions options)
        => string.Join(
            '\n',
            $"DenyHandling: {options.DenyHandling}",
            $"RejectRequestOn: {options.RejectRequestOn}",
            $"Schemes: {(options.Schemes is { } schemes ? string.Join(',', schemes) : "unset")}",
            $"SchemeChallenges: {DescribeChallenges(options.SchemeChallenges)}",
            $"ScopeClaimName: {options.ScopeClaimName}",
            $"ScopeClaimFormat: {options.ScopeClaimFormat}",
            $"EnableAttribution: {options.EnableAttribution}",
            $"DisableAuthorizationValidation: {options.DisableAuthorizationValidation}");

    private static string DescribeChallenges(ImmutableDictionary<string, string> challenges)
        => challenges.IsEmpty
            ? "none"
            : string.Join(',', challenges.Select(challenge => $"{challenge.Key}={challenge.Value}"));
}
