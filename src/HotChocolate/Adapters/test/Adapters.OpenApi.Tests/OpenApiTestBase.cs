using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HotChocolate.Adapters.OpenApi;

public abstract class OpenApiTestBase : IAsyncLifetime
{
    private const string TokenIssuer = "test-issuer";
    private const string TokenAudience = "test-audience";
    public const string AdminRole = "Admin";
    private static readonly SymmetricSecurityKey s_tokenKey = new("test-secret-key-at-least-32-bytes"u8.ToArray());

    private readonly TestServerSession _testServerSession = new();

    protected static TestOpenApiDefinitionStorage CreateBasicTestDefinitionStorage()
    {
        return new TestOpenApiDefinitionStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
                name
                email
              }
            }
            """,
            """
            query GetFullUser($userId: ID!, $includeAddress: Boolean!) @http(method: GET, route: "/users/{userId}/details", queryParameters: ["includeAddress"]) {
              userById(id: $userId) {
                id
                name
                email
                address @include(if: $includeAddress) {
                  ...UserAddress
                }
              }
            }
            """,
            """
            "Fetches all users"
            query GetUsers @http(method: GET, route: "/users") {
              users {
                id
              }
            }
            """,
            """
            "Creates a user"
            mutation CreateUser($user: UserInput! @body) @http(method: POST, route: "/users") {
              createUser(user: $user) {
                id
                name
                email
              }
            }
            """,
            """
            "Updates a user's details"
            mutation UpdateUser($user: UserInput! @body) @http(method: PUT, route: "/users/{userId:$user.id}") {
              updateUser(user: $user) {
                id
                name
                email
              }
            }
            """,
            """
            mutation UpdateDeeplyNestedObject($input: DeeplyNestedInput! @body)
              @http(method: PUT, route: "/object/{userId:$input.userId}", queryParameters: ["field:$input.object.field2"]) {
              updateDeeplyNestedObject(input: $input) {
                userId
                field
                object {
                  otherField
                  field2
                }
              }
            }
            """,
            """
            "The user's address"
            fragment UserAddress on Address {
              street
            }
            """,
            """
            query UnionQuery @http(method: GET, route: "/union") {
              withUnionType {
                petType: __typename
                ... on Cat {
                  isPurring
                }
                ... on Dog {
                  isBarking
                }
              }
            }
            """,
            """
            query InterfaceQuery @http(method: GET, route: "/interface") {
              withInterfaceType {
                petType: __typename
                name
                ... on Cat {
                  isPurring
                }
                ... on Dog {
                  isBarking
                }
              }
            }
            """,
            """
            query ComplexObjectQuery($input: ComplexObjectInput! @body) @http(method: POST, route: "/complex") {
              complexObject(input: $input) {
                any
                base64String
                boolean
                byte
                date
                dateTime
                decimal
                enum
                float
                id
                int
                json
                list
                localDate
                localDateTime
                localTime
                long
                object {
                  field1A {
                    field1B {
                      field1C
                    }
                  }
                }
                short
                string
                timeSpan
                unknown
                url
                uuid
              }
            }
            """);
    }

    public Task InitializeAsync()
    {
        return InitializeAsync(_testServerSession);
    }

    protected virtual Task InitializeAsync(TestServerSession serverSession) => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _testServerSession.Dispose();
        return Task.CompletedTask;
    }

    protected abstract void ConfigureStorage(
        IServiceCollection services,
        IOpenApiDefinitionStorage storage,
        OpenApiDiagnosticEventListener? eventListener);

    protected virtual void ConfigureOpenApi(OpenApiOptions options)
    {
        options.AddGraphQLTransformer();
    }

    protected virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApiEndpoints();
    }

    protected TestServer CreateTestServer(
        IOpenApiDefinitionStorage storage,
        OpenApiDiagnosticEventListener? eventListener = null)
    {
        return _testServerSession.CreateServer(
            services =>
            {
                services
                    .AddLogging()
                    .AddRouting();

                services.AddHeaderPropagation(options => options.Headers.Add("Authorization"));

                services
                    .AddAuthentication()
                    .AddJwtBearer(
                        o => o.TokenValidationParameters =
                            new TokenValidationParameters
                            {
                                ValidIssuer = TokenIssuer,
                                ValidAudience = TokenAudience,
                                IssuerSigningKey = s_tokenKey
                            });

                services.AddOpenApi(ConfigureOpenApi);

                ConfigureStorage(services, storage, eventListener);
            },
            app =>
            {
                app
                    .UseRouting()
                    .UseAuthentication()
                    .UseHeaderPropagation()
                    .UseEndpoints(endpoints =>
                    {
                        endpoints.MapOpenApi();

                        ConfigureEndpoints(endpoints);

                        endpoints.MapGraphQL();
                    });
            });
    }

    protected TestServer CreateSourceSchema()
    {
        return _testServerSession.CreateServer(
            services =>
            {
                services
                    .AddLogging()
                    .AddRouting();

                services
                    .AddAuthentication()
                    .AddJwtBearer(
                        o => o.TokenValidationParameters =
                            new TokenValidationParameters
                            {
                                ValidIssuer = TokenIssuer,
                                ValidAudience = TokenAudience,
                                IssuerSigningKey = s_tokenKey
                            });

                services.AddGraphQLServer()
                    .AddBasicServer();
            },
            app =>
            {
                app
                    .UseRouting()
                    .UseAuthentication()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL());
            });
    }

    protected static async Task<string> GetOpenApiDocumentAsync(HttpClient client)
    {
        var response = await client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    protected static async Task SpinWaitAsync(Func<Task<bool>> condition, CancellationToken cancellationToken)
    {
        while (!await condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(10, cancellationToken);
        }
    }

    protected sealed class TestOpenApiDiagnosticEventListener : OpenApiDiagnosticEventListener
    {
        public List<OpenApiDefinitionValidationError> Errors { get; } = [];

        public ManualResetEventSlim HasReportedErrors { get; } = new(false);

        public override void ValidationErrors(IReadOnlyList<OpenApiDefinitionValidationError> errors)
        {
            Errors.AddRange(errors);
            HasReportedErrors.Set();
        }
    }

    protected static class TestJwtTokenHelper
    {
        public static string GenerateToken(string? role = null)
        {
            Claim[] claims = [
                new(ClaimTypes.Name, "Test"),
                new(ClaimTypes.Role, role ?? AdminRole)];

            var token = new JwtSecurityToken(
                issuer: TokenIssuer,
                audience: TokenAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: new SigningCredentials(s_tokenKey, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    protected sealed class TestOpenApiDefinitionStorage : IOpenApiDefinitionStorage
    {
#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif
        private readonly Dictionary<string, IOpenApiDefinition> _definitionsById = [];

        public event EventHandler? Changed;

        public TestOpenApiDefinitionStorage(params IEnumerable<string>? documents)
        {
            if (documents is not null)
            {
                var index = 0;
                foreach (var document in documents)
                {
                    var documentNode = Utf8GraphQLParser.Parse(document);
                    var definition = OpenApiDefinitionParser.Parse(documentNode);

                    _definitionsById.Add(index.ToString(), definition);
                    index++;
                }
            }
        }

        public ValueTask<IEnumerable<IOpenApiDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return ValueTask.FromResult<IEnumerable<IOpenApiDefinition>>(_definitionsById.Values.ToList());
            }
        }

        public void AddOrUpdateDocument(string id, string document)
        {
            var documentNode = Utf8GraphQLParser.Parse(document);
            var definition = OpenApiDefinitionParser.Parse(documentNode);

            AddOrUpdateDefinition(id, definition);
        }

        public void AddOrUpdateDefinition(string id, IOpenApiDefinition definition)
        {
            lock (_lock)
            {
                _definitionsById[id] = definition;
                OnChanged();
            }
        }

        public void RemoveDocument(string id)
        {
            lock (_lock)
            {
                var removed = _definitionsById.Remove(id);

                if (removed)
                {
                    OnChanged();
                }
            }
        }

        private void OnChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
