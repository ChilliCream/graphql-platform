using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Authorization;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class AuthorizationHttpTests : FusionTestBase
{
    private const string SimpleSchema =
        """
        type Query {
          field: String
        }
        """;

    private const string ProtectedSchema =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        type Query {
          field: String @authenticated
        }
        """;

    private static readonly ImmutableDictionary<string, string> s_cookieChallenges =
        ImmutableDictionary<string, string>.Empty.Add("Cookies", "Cookie").Add("Session", "Session");

    [Fact]
    public async Task Request_Should_ChallengeWithMappedChallenges_When_EscalatedAndSchemesAreUnset()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieSchemes,
            configureGatewayBuilder: b => UseEscalation(
                b.ModifyAuthorizationOptions(o => o.SchemeChallenges = s_cookieChallenges),
                HttpStatusCode.Unauthorized));

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Cookie, Session", GetChallenge(response));
    }

    [Fact]
    public async Task Request_Should_ChallengeWithListedSchemesOnly_When_EscalatedAndSchemesAreSet()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieSchemes,
            configureGatewayBuilder: b => UseEscalation(
                b.ModifyAuthorizationOptions(
                    o =>
                    {
                        o.Schemes = ImmutableArray.Create("Session");
                        o.SchemeChallenges = s_cookieChallenges;
                    }),
                HttpStatusCode.Unauthorized));

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Session", GetChallenge(response));
    }

    [Fact]
    public async Task Request_Should_ChallengeWithBearer_When_JwtHandlerIsRegisteredUnderCustomName()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: services => services.AddAuthentication().AddJwtBearer("MyJwt", _ => { }),
            configureGatewayBuilder: b => UseEscalation(b, HttpStatusCode.Unauthorized));

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", GetChallenge(response));
    }

    [Fact]
    public async Task Request_Should_ChallengeWithBearerOnce_When_TwoJwtHandlersAreRegistered()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: services => services
                .AddAuthentication()
                .AddJwtBearer("JwtA", _ => { })
                .AddJwtBearer("JwtB", _ => { }),
            configureGatewayBuilder: b => UseEscalation(b, HttpStatusCode.Unauthorized));

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", GetChallenge(response));
    }

    [Fact]
    public async Task Request_Should_OmitChallenge_When_EscalatedAndHandlerHasNoChallengeEntry()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieSchemes,
            configureGatewayBuilder: b => UseEscalation(b, HttpStatusCode.Unauthorized));

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(GetChallenge(response));
    }

    [Fact]
    public async Task Request_Should_OmitChallenge_When_EscalatedAndNoSchemeIsRegistered()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => UseEscalation(b, HttpStatusCode.Unauthorized));

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(GetChallenge(response));
    }

    [Fact]
    public async Task Request_Should_ReturnForbiddenWithoutChallenge_When_EscalatedAsUnauthorized()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieSchemes,
            configureGatewayBuilder: b => UseEscalation(b, HttpStatusCode.Forbidden));

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(GetChallenge(response));
    }

    [Fact]
    public async Task Request_Should_ReturnOk_When_ResultIsNotEscalated()
    {
        // arrange
        using var server = CreateSourceSchema("A", SimpleSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieSchemes);

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(GetChallenge(response));
    }

    [Fact]
    public async Task Startup_Should_Fail_When_SchemaUsesAuthorizationAndNoAuthenticationIsRegistered()
    {
        // arrange
        using var server = CreateSourceSchema("A", ProtectedSchema);

        // act
        var act = async () => await CreateCompositeSchemaAsync([("A", server)]);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(
            "The schema uses authorization directives but no authentication scheme is registered. "
            + "Register an authentication scheme, or disable the authorization validation.",
            exception.Message);
    }

    [Fact]
    public async Task Gateway_Should_ReturnOk_When_SchemaUsesAuthorizationAndSchemeIsRegistered()
    {
        // arrange
        using var server = CreateSourceSchema("A", ProtectedSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieSchemes);

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Startup_Should_Fail_When_ListedSchemeIsNotRegistered()
    {
        // arrange
        using var server = CreateSourceSchema("A", ProtectedSchema);

        // act
        var act = async () => await CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddCookieSchemes,
            configureGatewayBuilder: b => b.ModifyAuthorizationOptions(
                o => o.Schemes = ImmutableArray.Create("Session", "Bearer")));

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(
            "The authentication scheme 'Bearer' of the authorization options is not registered. "
            + "Register it, remove it from the schemes, or disable the authorization validation.",
            exception.Message);
    }

    [Fact]
    public async Task Gateway_Should_ReturnOk_When_ValidationIsDisabledAndNoAuthenticationIsRegistered()
    {
        // arrange
        using var server = CreateSourceSchema("A", ProtectedSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyAuthorizationOptions(
                o => o.DisableAuthorizationValidation = true));

        // act
        using var response = await PostAsync(gateway);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static IFusionGatewayBuilder UseEscalation(
        IFusionGatewayBuilder builder,
        HttpStatusCode statusCode)
        => builder.UseRequest(
            (_, _) => async context =>
            {
                var schemeResolver = context.Schema.Services.GetRequiredService<AuthenticationSchemeResolver>();
                var result = OperationResult.FromError(ErrorBuilder.New().SetMessage("Escalated.").Build());
                result.ContextData = result.ContextData.Add(ExecutionContextData.HttpStatusCode, statusCode);

                if (await schemeResolver.GetChallengeAsync(context.RequestAborted) is { } challenge)
                {
                    result.ContextData = result.ContextData.Add(
                        ExecutionContextData.WwwAuthenticateHeaderValue,
                        challenge);
                }

                context.Result = result;
            },
            before: WellKnownRequestMiddleware.OperationExecutionMiddleware,
            allowMultiple: true);

    private static void AddCookieSchemes(IServiceCollection services)
        => services
            .AddAuthentication()
            .AddCookie("Cookies")
            .AddCookie("Session");

    private static async Task<HttpResponseMessage> PostAsync(Gateway gateway)
    {
        using var client = gateway.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/graphql")
        {
            Content = new StringContent("""{"query":"{ field }"}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/graphql-response+json"));

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string? GetChallenge(HttpResponseMessage response)
        => response.Headers.TryGetValues("WWW-Authenticate", out var values)
            ? string.Join(", ", values)
            : null;
}
