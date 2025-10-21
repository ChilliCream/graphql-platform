using System.Collections.Immutable;
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

namespace HotChocolate.Exporters.OpenApi;

public abstract class OpenApiTestBase
{
    protected static TestOpenApiDocumentStorage CreateBasicTestDocumentStorage()
    {
        return new TestOpenApiDocumentStorage(
            """
            "Fetches a user by their id"
            query GetUserById($userId: ID!) @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
                name
                email
              }
            }
            """);
    }

    protected static TestServer CreateBasicTestServer(IOpenApiDocumentStorage storage)
    {
        return CreateTestServer(
            configureRequestExecutor: b => b
                .AddOpenApiDocumentStorage(storage)
                .AddQueryType<BasicServer.QueryType>(),
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

                services.AddOpenApi(options => configureOpenApi?.Invoke(options));

                var executor = services
                    .AddGraphQLServer();

                configureRequestExecutor?.Invoke(executor);
            })
            .Configure(app => app
                .UseRouting()
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

    private static class BasicServer
    {
        public class QueryType
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

                return new User(id, "User " + id, id + "@example.com");
            }
        }

        public sealed record User([property: ID] int Id, string Name, string Email);
    }

    protected sealed class TestOpenApiDocumentStorage : IOpenApiDocumentStorage, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);
        private readonly Dictionary<string, OpenApiDocumentDefinition> _documentsById = [];
        private ImmutableList<ObserverSession> _sessions = [];
        private bool _disposed;
        private readonly object _sync = new();

        public TestOpenApiDocumentStorage(params IEnumerable<string>? documents)
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

        public IDisposable Subscribe(IObserver<OpenApiDocumentStorageEventArgs> observer)
        {
            return new ObserverSession(this, observer);
        }

        public async Task AddOrUpdateDocumentAsync(
            string id,
            DocumentNode document,
            CancellationToken cancellationToken = default)
        {
            var operation = document.Definitions.OfType<OperationDefinitionNode>().FirstOrDefault();

            if (operation is null)
            {
                throw new ArgumentException($"Document {document} has no operation definition");
            }

            OpenApiDocumentStorageEventType type;
            await _semaphore.WaitAsync(cancellationToken);

            OpenApiDocumentDefinition tool;
            try
            {
                tool = new OpenApiDocumentDefinition(id, document);
                if (_documentsById.TryAdd(id, tool))
                {
                    type = OpenApiDocumentStorageEventType.Added;
                }
                else
                {
                    _documentsById[id] = tool;
                    type = OpenApiDocumentStorageEventType.Modified;
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
                NotifySubscribers(id, null, OpenApiDocumentStorageEventType.Removed);
            }
        }

        private void NotifySubscribers(
            string id,
            OpenApiDocumentDefinition? toolDefinition,
            OpenApiDocumentStorageEventType type)
        {
            if (type is OpenApiDocumentStorageEventType.Added or OpenApiDocumentStorageEventType.Modified)
            {
                ArgumentNullException.ThrowIfNull(toolDefinition);
            }

            if (_disposed)
            {
                return;
            }

            var sessions = _sessions;
            var eventArgs = new OpenApiDocumentStorageEventArgs(id, type, toolDefinition);

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
            private readonly TestOpenApiDocumentStorage _storage;
            private readonly IObserver<OpenApiDocumentStorageEventArgs> _observer;

            public ObserverSession(
                TestOpenApiDocumentStorage storage,
                IObserver<OpenApiDocumentStorageEventArgs> observer)
            {
                _storage = storage;
                _observer = observer;

                lock (storage._sync)
                {
                    _storage._sessions = _storage._sessions.Add(this);
                }
            }

            public void Notify(OpenApiDocumentStorageEventArgs eventArgs)
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
