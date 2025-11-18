using System.Collections.Immutable;
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

    protected static TestOpenApiDefinitionStorage CreateBasicTestDocumentStorage()
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

    protected sealed class TestOpenApiDiagnosticEventListener : OpenApiDiagnosticEventListener
    {
        public List<IOpenApiError> Errors { get; } = [];

        public ManualResetEventSlim HasReportedErrors { get; } = new(false);

        public override void ValidationErrors(IReadOnlyList<IOpenApiError> errors)
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

    protected sealed class TestOpenApiDefinitionStorage : IOpenApiDefinitionStorage, IDisposable
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<string, OpenApiDocumentDefinition> _documentsById = [];
        private ImmutableList<ObserverSession> _sessions = [];
        private bool _disposed;
        private readonly object _sync = new();

        public TestOpenApiDefinitionStorage(params IEnumerable<string>? documents)
        {
            if (documents is not null)
            {
                var i = 0;
                foreach (var document in documents)
                {
                    var id = i++.ToString();
                    var parsed = Utf8GraphQLParser.Parse(document);

                    _documentsById.Add(id, new OpenApiDocumentDefinition(id, parsed));
                }
            }
        }

        public ValueTask<IEnumerable<OpenApiDocumentDefinition>> GetDocumentsAsync(
            CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var documents = _documentsById.Values.ToList();

                return ValueTask.FromResult<IEnumerable<OpenApiDocumentDefinition>>(documents);
            }
        }

        public IDisposable Subscribe(IObserver<OpenApiDefinitionStorageEventArgs> observer)
        {
            return new ObserverSession(this, observer);
        }

        public void AddOrUpdateDocument(string id, string document)
        {
            lock (_lock)
            {
                var parsedDocument = Utf8GraphQLParser.Parse(document);

                var documentDefinition = new OpenApiDocumentDefinition(id, parsedDocument);
                OpenApiDefinitionStorageEventType type;
                if (_documentsById.TryAdd(id, documentDefinition))
                {
                    type = OpenApiDefinitionStorageEventType.Added;
                }
                else
                {
                    _documentsById[id] = documentDefinition;
                    type = OpenApiDefinitionStorageEventType.Modified;
                }

                NotifySubscribers(id, documentDefinition, type);
            }
        }

        public void RemoveDocument(string id)
        {
            lock (_lock)
            {
                var removed = _documentsById.Remove(id);

                if (removed)
                {
                    NotifySubscribers(id, null, OpenApiDefinitionStorageEventType.Removed);
                }
            }
        }

        private void NotifySubscribers(
            string id,
            OpenApiDocumentDefinition? toolDefinition,
            OpenApiDefinitionStorageEventType type)
        {
            if (type is OpenApiDefinitionStorageEventType.Added or OpenApiDefinitionStorageEventType.Modified)
            {
                ArgumentNullException.ThrowIfNull(toolDefinition);
            }

            if (_disposed)
            {
                return;
            }

            var sessions = _sessions;
            var eventArgs = new OpenApiDefinitionStorageEventArgs(id, type, toolDefinition);

            foreach (var session in sessions)
            {
                session.Notify(eventArgs);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_sync)
                {
                    foreach (var session in _sessions)
                    {
                        session.Dispose();
                    }

                    _sessions = [];
                    _disposed = true;
                }
            }
        }

        private sealed class ObserverSession : IDisposable
        {
            private bool _disposed;
            private readonly TestOpenApiDefinitionStorage _storage;
            private readonly IObserver<OpenApiDefinitionStorageEventArgs> _observer;

            public ObserverSession(
                TestOpenApiDefinitionStorage storage,
                IObserver<OpenApiDefinitionStorageEventArgs> observer)
            {
                _storage = storage;
                _observer = observer;

                lock (storage._sync)
                {
                    _storage._sessions = _storage._sessions.Add(this);
                }
            }

            public void Notify(OpenApiDefinitionStorageEventArgs eventArgs)
            {
                if (!_disposed && !_storage._disposed)
                {
                    _observer.OnNext(eventArgs);
                }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                lock (_storage._sync)
                {
                    _storage._sessions = _storage._sessions.Remove(this);
                }

                _disposed = true;
            }
        }
    }
}
