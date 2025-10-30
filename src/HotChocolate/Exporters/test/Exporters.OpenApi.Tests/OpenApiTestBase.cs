using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Exporters.OpenApi.Extensions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HotChocolate.Exporters.OpenApi;

public abstract class OpenApiTestBase
{
    private const string TokenIssuer = "test-issuer";
    private const string TokenAudience = "test-audience";
    private const string AdminRole = "Admin";
    private static readonly SymmetricSecurityKey s_tokenKey = new("test-secret-key-at-least-32-bytes"u8.ToArray());

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
            """);
    }

    protected static TestServer CreateBasicTestServer(IOpenApiDefinitionStorage storage)
    {
        return CreateTestServer(
            configureRequestExecutor: b => b
                .AddOpenApiDefinitionStorage(storage)
                .AddAuthorization()
                .AddQueryType<BasicServer.Query>()
                .AddMutationType<BasicServer.Mutation>(),
            configureOpenApi: o => o.AddGraphQL(),
            configureEndpoints: e => e.MapGraphQLEndpoints());
    }

    protected static TestServer CreateTestServer(
        Action<IRequestExecutorBuilder>? configureRequestExecutor = null,
        Action<OpenApiOptions>? configureOpenApi = null,
        Action<IEndpointRouteBuilder>? configureEndpoints = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
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

                services.AddOpenApi(options => configureOpenApi?.Invoke(options));

                var executor = services
                    .AddGraphQLServer();

                configureRequestExecutor?.Invoke(executor);
            })
            .Configure(app => app
                .UseRouting()
                .UseAuthentication()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapOpenApi();

                    configureEndpoints?.Invoke(endpoints);
                }));

        return new TestServer(builder);
    }

    protected static async Task<string> GetOpenApiDocumentAsync(HttpClient client)
    {
        var response = await client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
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

    private static class BasicServer
    {
        public class Query
        {
            public User? GetUserById([ID] int id, IResolverContext context)
            {
                if (id == 5)
                {
                    throw new GraphQLException(
                        ErrorBuilder.New()
                            .SetMessage("Something went wrong")
                            .SetPath(context.Path)
                            .Build());
                }

                if (id < 1 || id > 3)
                {
                    return null;
                }

                return new User(id);
            }

            [Authorize(Roles = [AdminRole])]
            public IEnumerable<User> GetUsers()
                => [new User(1), new User(2), new User(3)];
        }

        public class Mutation
        {
            public User CreateUser(UserInput user)
            {
                return new User(user.Id);
            }

            public User UpdateUser(UserInput user)
            {
                return CreateUser(user);
            }
        }

        public class UserInput
        {
            [ID]
            public int Id { get; init; }

            public required string Name { get; init; }

            public required string Email { get; init; }
        }

        public sealed class User(int id)
        {
            [ID]
            public int Id { get; init; } = id;

            public string Name { get; set; } = "User " + id;

            public string? Email { get; set; } = id + "@example.com";

            public Address Address { get; set; } = new Address(id + " Street");
        }

        public sealed record Address(string Street);
    }

    protected sealed class TestOpenApiDefinitionStorage : IOpenApiDefinitionStorage, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);
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

        public async ValueTask<IEnumerable<OpenApiDocumentDefinition>> GetDocumentsAsync(
            CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                return _documentsById.Values.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public IDisposable Subscribe(IObserver<OpenApiDefinitionStorageEventArgs> observer)
        {
            return new ObserverSession(this, observer);
        }

        public async Task AddOrUpdateDocumentAsync(
            string id,
            DocumentNode document,
            CancellationToken cancellationToken = default)
        {
            OpenApiDefinitionStorageEventType type;
            await _semaphore.WaitAsync(cancellationToken);

            OpenApiDocumentDefinition tool;
            try
            {
                tool = new OpenApiDocumentDefinition(id, document);
                if (_documentsById.TryAdd(id, tool))
                {
                    type = OpenApiDefinitionStorageEventType.Added;
                }
                else
                {
                    _documentsById[id] = tool;
                    type = OpenApiDefinitionStorageEventType.Modified;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            NotifySubscribers(id, tool, type);
        }

        public async Task RemoveDocumentAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            bool removed;
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                removed = _documentsById.Remove(id);
            }
            finally
            {
                _semaphore.Release();
            }

            if (removed)
            {
                NotifySubscribers(id, null, OpenApiDefinitionStorageEventType.Removed);
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
