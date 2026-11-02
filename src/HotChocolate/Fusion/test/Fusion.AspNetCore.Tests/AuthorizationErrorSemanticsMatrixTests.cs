using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Policies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class AuthorizationErrorSemanticsMatrixTests : FusionTestBase
{
    private const string UserHeader = "X-Test-User";
    private const string Member = "member";
    private const string Reader = "reader";

    private const string MatrixSchema =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        type Query {
          open: String
          me: String @authenticated
          admin: String @requiresScopes(scopes: [["admin"]])
          guarded: String @policy(policies: [["finance"]])
          product: Product
          strictProduct: Product!
          products: [Product]
          strictProducts: [Product!]
        }

        type Product {
          id: ID!
          name: String @authenticated
          price: Int @requiresScopes(scopes: [["read"]])
          cost: Int! @policy(policies: [["finance"]])
        }
        """;

    private static readonly ImmutableArray<(string Name, string Query, string? User)> s_fieldCells =
    [
        ("Unauthenticated nullable field", "{ me }", null),
        ("Unauthorized nullable field", "{ admin }", Member),
        ("Anonymous principal on a scope-protected field", "{ admin }", null),
        ("Denied policy on nullable field", "{ guarded }", Reader),
        ("Allowed selections", "{ product { id name price } }", Reader),
        ("Partial data beside a denied field", "{ open me }", null),
        ("Denied non-null field under a nullable parent", "{ product { id cost } }", Reader),
        ("Denied non-null field under a non-null parent", "{ strictProduct { id cost } }", Reader),
        ("Denied nullable field in every list element", "{ products { id price } }", Member),
        ("Denied non-null field in nullable list elements, one error per element", "{ products { id cost } }", Reader),
        (
            "Denied non-null field in non-null list elements, "
                + "one error at the first violated index and the list is nulled",
            "{ strictProducts { id cost } }",
            Reader)
    ];

    private static readonly ImmutableArray<(string Name, string Query, string? User)> s_escalationCells =
    [
        ("Unauthenticated denial", "{ open me }", null),
        ("Unauthorized denial", "{ open admin }", Member),
        ("Denied non-null field", "{ product { id cost } }", Reader),
        ("Nothing denied", "{ open product { id price } }", Reader),
        ("Unprotected operation", "{ open }", null)
    ];

    [Fact]
    public async Task Response_Should_RenderSilentNulls_When_DenyHandlingIsNull()
    {
        // arrange
        var snapshot = Snapshot.Create();
        using var server = CreateSourceSchema("A", MatrixSchema);
        using var gateway = await CreateGatewayAsync(
            server,
            o => o.DenyHandling = DenyHandling.Null);

        // act
        await AddScenarioAsync(snapshot, gateway, "Null", s_fieldCells);

        // assert
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Response_Should_RenderIndexedFieldErrors_When_DenyHandlingIsError()
    {
        // arrange
        var snapshot = Snapshot.Create();
        using var server = CreateSourceSchema("A", MatrixSchema);
        using var gateway = await CreateGatewayAsync(
            server,
            o => o.DenyHandling = DenyHandling.Error);

        // act
        await AddScenarioAsync(snapshot, gateway, "Error", s_fieldCells);

        // assert
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Response_Should_RenderAttributionOnlyOnAuthorizationErrors_When_AttributionIsOn()
    {
        // arrange
        var snapshot = Snapshot.Create();
        using var server = CreateSourceSchema("A", MatrixSchema);
        using var errorGateway = await CreateGatewayAsync(
            server,
            o =>
            {
                o.DenyHandling = DenyHandling.Error;
                o.EnableAttribution = true;
            });
        using var nullGateway = await CreateGatewayAsync(
            server,
            o =>
            {
                o.DenyHandling = DenyHandling.Null;
                o.EnableAttribution = true;
            });

        // act
        await AddScenarioAsync(snapshot, errorGateway, "Error with attribution", s_fieldCells);
        await AddScenarioAsync(snapshot, nullGateway, "Null with attribution", s_fieldCells);

        // assert
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Response_Should_RejectRequestOnlyForUnauthenticated_When_RejectRequestOnIsOnUnauthenticated()
    {
        // arrange
        var snapshot = Snapshot.Create();
        using var server = CreateSourceSchema("A", MatrixSchema);
        using var gateway = await CreateGatewayAsync(
            server,
            o =>
            {
                o.DenyHandling = DenyHandling.Error;
                o.RejectRequestOn = RejectRequestOn.OnUnauthenticated;
            });
        using var attributedGateway = await CreateGatewayAsync(
            server,
            o =>
            {
                o.DenyHandling = DenyHandling.Error;
                o.RejectRequestOn = RejectRequestOn.OnUnauthenticated;
                o.EnableAttribution = true;
            });

        // act
        await AddScenarioAsync(snapshot, gateway, "OnUnauthenticated", s_escalationCells);
        await AddScenarioAsync(
            snapshot,
            attributedGateway,
            "OnUnauthenticated with attribution",
            s_escalationCells);

        // assert
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Response_Should_RejectRequestForEveryDenial_When_RejectRequestOnIsOnUnauthorized()
    {
        // arrange
        var snapshot = Snapshot.Create();
        using var server = CreateSourceSchema("A", MatrixSchema);
        using var gateway = await CreateGatewayAsync(
            server,
            o =>
            {
                o.DenyHandling = DenyHandling.Error;
                o.RejectRequestOn = RejectRequestOn.OnUnauthorized;
            });
        using var attributedGateway = await CreateGatewayAsync(
            server,
            o =>
            {
                o.DenyHandling = DenyHandling.Error;
                o.RejectRequestOn = RejectRequestOn.OnUnauthorized;
                o.EnableAttribution = true;
            });
        using var nullGateway = await CreateGatewayAsync(
            server,
            o =>
            {
                o.DenyHandling = DenyHandling.Null;
                o.RejectRequestOn = RejectRequestOn.OnUnauthorized;
            });

        // act
        await AddScenarioAsync(snapshot, gateway, "OnUnauthorized", s_escalationCells);
        await AddScenarioAsync(
            snapshot,
            attributedGateway,
            "OnUnauthorized with attribution",
            s_escalationCells);
        await AddScenarioAsync(
            snapshot,
            nullGateway,
            "OnUnauthorized with null deny handling",
            s_escalationCells);

        // assert
        snapshot.MatchMarkdownSnapshot();
    }

    private Task<Gateway> CreateGatewayAsync(TestServer server, Action<FusionAuthorizationOptions> configure)
        => CreateCompositeSchemaAsync(
            [("A", server)],
            configureServices: AddJwtScheme,
            configureApplication: UseTestUser,
            configureGatewayBuilder: b => b
                .ModifyAuthorizationOptions(configure)
                .AddInMemoryPolicies(p => p.Deny("finance")),
            includeOperationPlan: false);

    private static async Task AddScenarioAsync(
        Snapshot snapshot,
        Gateway gateway,
        string scenario,
        ImmutableArray<(string Name, string Query, string? User)> cells)
    {
        using var client = gateway.CreateClient();

        foreach (var (name, query, user) in cells)
        {
            using var response = await PostAsync(client, query, user);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var challenge = response.Headers.TryGetValues("WWW-Authenticate", out var values)
                ? $", WWW-Authenticate: {string.Join(", ", values)}"
                : string.Empty;
            var status = $"{(int)response.StatusCode} {response.StatusCode}{challenge}";
            var title = $"{scenario}: {name} ({user ?? "anonymous"}) -> {status}";

            snapshot.Add(Format(body), title, MarkdownLanguages.Json);
        }
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string query, string? user)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/graphql")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { query }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/graphql-response+json"));

        if (user is not null)
        {
            request.Headers.Add(UserHeader, user);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static string Format(string body)
    {
        using var document = JsonDocument.Parse(body);
        return JsonSerializer.Serialize(
            document.RootElement,
            new JsonSerializerOptions { WriteIndented = true });
    }

    private static void UseTestUser(IApplicationBuilder app)
    {
        app.UseRouting();
        app.Use(
            async (context, next) =>
            {
                if (context.Request.Headers.TryGetValue(UserHeader, out var user))
                {
                    var claims = user == Reader ? [new Claim("scope", "read")] : Array.Empty<Claim>();
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
                }

                await next(context);
            });
        app.UseEndpoints(endpoint => endpoint.MapGraphQL());
    }

    private static void AddJwtScheme(IServiceCollection services)
        => services
            .AddAuthentication()
            .AddJwtBearer("Jwt", _ => { });
}
