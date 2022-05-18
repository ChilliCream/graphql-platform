using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using GreenDonut;
using HotChocolate.ConferencePlanner.Attendees;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate.ConferencePlanner.Imports;
using HotChocolate.ConferencePlanner.Sessions;
using HotChocolate.ConferencePlanner.Speakers;
using HotChocolate.ConferencePlanner.Tracks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Newtonsoft.Json;

namespace HotChocolate.ConferencePlanner
{
    public class BenchmarkBase
    {
        private static readonly MD5DocumentHashProvider _md5 = new();
        private static readonly JsonSerializerOptions _options =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        private readonly Uri _url;

        public BenchmarkBase()
        {
            var servicesCollection = new ServiceCollection();
            ConfigureServices(servicesCollection);
            Services = servicesCollection.BuildServiceProvider();
            ExecutorResolver = Services.GetRequiredService<IRequestExecutorResolver>();

            TestServerFactory.CreateServer(
                services =>
                {
                    services.AddGraphQLServer();
                    ConfigureServices(services);
                },
                out var port);

            _url = new Uri($"http://localhost:{port}/graphql");
            TestClient = new HttpClient();
        }

        public IServiceProvider Services { get; }

        public IRequestExecutorResolver ExecutorResolver { get; }

        public HttpClient TestClient { get; }

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            foreach (MethodInfo method in GetType().GetMethods()
                .Where(t => t.IsDefined(typeof(BenchmarkAttribute))))
            {
                Console.WriteLine("Initialize: " + method.Name);
                if (method.Invoke(this, Array.Empty<object>()) is Task task)
                {
                    await task.ConfigureAwait(false);
                }
            }
        }

        public Task BenchmarkServerAsync(string requestDocument)
            => BenchmarkServerAsync(new ClientQueryRequest { Query = requestDocument });

        public Task BenchmarkServerAsync(ClientQueryRequest request)
            => PostAsync(request);

        public Task BenchmarkAsync(string requestDocument)
        {
            return BenchmarkAsync(new QueryRequest(new QuerySourceText(requestDocument)));
        }

        public async Task BenchmarkAsync(IQueryRequest request)
        {
            IRequestExecutor executor = await ExecutorResolver.GetRequestExecutorAsync();
            IExecutionResult result = await executor.ExecuteAsync(request);

            if (result is IQueryResult cr && cr.Errors is { Count: > 0 })
            {
                throw new InvalidOperationException("The request failed.");
            }

            if (result is IAsyncDisposable d)
            {
                await d.DisposeAsync();
            }
        }

        public async Task<string> PrintQueryPlanAsync(string requestDocument)
        {
            IRequestExecutor executor = await ExecutorResolver.GetRequestExecutorAsync();

            string hash = _md5.ComputeHash(Encoding.UTF8.GetBytes(requestDocument).AsSpan());
            DocumentNode document = Utf8GraphQLParser.Parse(requestDocument);
            var operation = document.Definitions.OfType<OperationDefinitionNode>().First();

            IPreparedOperation preparedOperation =
                OperationCompiler.Compile(
                    hash,
                    document,
                    operation,
                    executor.Schema,
                    executor.Schema.GetOperationType(operation.Operation),
                    new InputParser());

            string serialized = preparedOperation.Print();
            Console.WriteLine(serialized);
            return serialized;
        }

        protected static IQueryRequest Prepare(string requestDocument)
        {
            string hash = _md5.ComputeHash(Encoding.UTF8.GetBytes(requestDocument).AsSpan());
            DocumentNode document = Utf8GraphQLParser.Parse(requestDocument);

            return QueryRequestBuilder.New()
                .SetQuery(document)
                .SetQueryHash(hash)
                .SetQueryId(hash)
                .Create();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services
                // First we add the DBContext which we will be using to interact with our
                // Database.
                .AddPooledDbContextFactory<ApplicationDbContext>(
                    options => options.UseSqlite("Data Source=conferences.db"))

                // This adds the GraphQL server core service and declares a schema.
                .AddGraphQL()

                    // Next we add the types to our schema.
                    .AddQueryType()
                        .AddTypeExtension<AttendeeQueries>()
                        .AddTypeExtension<SessionQueries>()
                        .AddTypeExtension<SpeakerQueries>()
                        .AddTypeExtension<TrackQueries>()
                    .AddMutationType()
                        .AddTypeExtension<AttendeeMutations>()
                        .AddTypeExtension<SessionMutations>()
                        .AddTypeExtension<SpeakerMutations>()
                        .AddTypeExtension<TrackMutations>()
                    .AddSubscriptionType()
                        .AddTypeExtension<AttendeeSubscriptions>()
                        .AddTypeExtension<SessionSubscriptions>()
                    .AddTypeExtension<AttendeeExtensions>()
                    .AddTypeExtension<SessionExtensions>()
                    .AddTypeExtension<TrackExtensions>()
                    .AddTypeExtension<SpeakerExtensions>()

                    // In this section we are adding extensions like relay helpers,
                    // filtering and sorting.
                    .AddFiltering()
                    .AddSorting()
                    .AddGlobalObjectIdentification()
                    .AddQueryFieldToMutationPayloads()
                    .AddIdSerializer()

                    // Now we add some the DataLoader to our system.
                    .AddDataLoader<AttendeeByIdDataLoader>()
                    .AddDataLoader<SessionByIdDataLoader>()
                    .AddDataLoader<SessionBySpeakerIdDataLoader>()
                    .AddDataLoader<SpeakerByIdDataLoader>()
                    .AddDataLoader<SpeakerBySessionIdDataLoader>()
                    .AddDataLoader<TrackByIdDataLoader>()

                    // .AddDiagnosticEventListener<BatchDataLoaderDiagnostics>()
                    // .AddDiagnosticEventListener<BatchExecutionDiagnostics>()

                    // we make sure that the db exists and prefill it with conference data.
                    .EnsureDatabaseIsCreated()

                    // Since we are using subscriptions, we need to register a pub/sub system.
                    // for our demo we are using a in-memory pub/sub system.
                    .AddInMemorySubscriptions();
        }

        private async Task PostAsync(ClientQueryRequest request)
        {
            using var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request, _options),
                Encoding.UTF8,
                "application/json");

            using var requestMsg = new HttpRequestMessage(HttpMethod.Post, _url)
            {
                Content = content
            };

            using HttpResponseMessage responseMsg = await TestClient.SendAsync(requestMsg);

            if (responseMsg.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Failed.");
            }
        }
    }

    public class BatchDataLoaderDiagnostics : DataLoaderDiagnosticEventListener
    {
        private static int _batches = 0;

        public static int Batches { get => _batches;  set => _batches = value; }

        public override IDisposable ExecuteBatch<TKey>(IDataLoader dataLoader, IReadOnlyList<TKey> keys)
        {
            Interlocked.Increment(ref _batches);
            // Console.WriteLine($"ExecuteBatch {dataLoader.GetType().Name} {string.Join(", ", keys)}.");
            return EmptyScope;
        }
    }

    public class BatchExecutionDiagnostics : ExecutionDiagnosticEventListener
    {
        public static int Starts { get; set;}

        public override IDisposable ExecuteRequest(IRequestContext context)
        {
            BatchDataLoaderDiagnostics.Batches = 0;
            var scope = new RequestScope();
            context.ContextData[nameof(RequestScope)] = scope;
            context.ContextData[nameof(Stopwatch)] = scope.Stopwatch;
            return scope;
        }

        public override IDisposable DispatchBatch(
            IRequestContext context)
        {
            return new BatchScope(((RequestScope)context.ContextData[nameof(RequestScope)]!));
        }

        public override void StartProcessing(IRequestContext context)
        {
            ((RequestScope)context.ContextData[nameof(RequestScope)]!).CountStarts();

            // TimeSpan timeSpan = ((RequestScope)context.ContextData[nameof(RequestScope)]!).Elapsed;
            // Console.WriteLine($"{timeSpan} Start processing.");
        }

        public override void StopProcessing(IRequestContext context)
        {
            // TimeSpan timeSpan = ((RequestScope)context.ContextData[nameof(RequestScope)]!).Elapsed;
            // Console.WriteLine($"{timeSpan} Stop processing.");
        }

        public override IDisposable ResolveFieldValue(IMiddlewareContext context)
        {
            return new ResolverScope(((RequestScope)context.ContextData[nameof(RequestScope)]!), (ISelection)context.Selection);
        }

        private sealed class RequestScope : IDisposable
        {
            private readonly Stopwatch _stopwatch;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<FieldCoordinate, int> _ = new();
            private int _starts;

            public RequestScope()
            {
                _stopwatch = Stopwatch.StartNew();
                Console.WriteLine($"{DateTime.Now} Execute Request.");
            }

            public TimeSpan Elapsed => _stopwatch.Elapsed;

            public Stopwatch Stopwatch => _stopwatch;

            public void Count(FieldCoordinate coordinate)
            {
                _.AddOrUpdate(coordinate, c => 1, (c, v) => v + 1);
            }

            public void CountStarts()
            {
                Interlocked.Add(ref _starts, 1);
            }

            public void Dispose()
            {
                Console.WriteLine($"Completed in {Elapsed}");

                Console.WriteLine("-----------------------------------");

                foreach (var field in _.OrderBy(t => t.Key.ToString()))
                {
                    Console.WriteLine($"{field.Key}:{field.Value}");
                }

                Console.WriteLine("-----------------------------------");

                Console.WriteLine($"Fields:{_.Select(t => t.Value).Sum()}");
                Console.WriteLine($"Starts:{_starts}");
                BatchExecutionDiagnostics.Starts = _starts;

                Console.WriteLine("-----------------------------------");
                Console.WriteLine("-----------------------------------");

                _stopwatch.Stop();
            }
        }

        private sealed class BatchScope : IDisposable
        {
            private RequestScope _requestScope;

            public BatchScope(RequestScope requestScope)
            {
                _requestScope = requestScope;
                // Console.WriteLine($"{_requestScope.Elapsed} Begin Dispatching Batch.");
            }

            public void Dispose()
            {
                // Console.WriteLine($"{_requestScope.Elapsed} End Dispatching Batch.");
            }
        }

        private sealed class ResolverScope : IDisposable
        {
            private RequestScope _requestScope;
            private ISelection _selection;

            public ResolverScope(RequestScope requestScope, ISelection selection)
            {
                _requestScope = requestScope;
                _selection = selection;

                TimeSpan timeSpan = requestScope.Elapsed;
                requestScope.Count(selection.Field.Coordinate);
                // Console.WriteLine($"{timeSpan} {Thread.CurrentThread.ManagedThreadId} Begin {selection.Field.Coordinate}");
            }

            public void Dispose()
            {
                TimeSpan timeSpan = _requestScope.Elapsed;
                //Console.WriteLine($"{timeSpan} {Thread.CurrentThread.ManagedThreadId} End {_selection.Field.Coordinate}");
            }
        }
    }

    public class ClientQueryRequest
    {
        public string? Id { get; set; }

        public string? OperationName { get; set; }

        public string? Query { get; set; }

        public Dictionary<string, object>? Variables { get; set; }

        public Dictionary<string, object>? Extensions { get; set; }
    }

    public class ClientQueryResponse
    {
        public string ContentType { get; set; } = default!;

        public HttpStatusCode StatusCode { get; set; }

        public Dictionary<string, object>? Data { get; set; }

        public List<Dictionary<string, object>>? Errors { get; set; }

        public Dictionary<string, object>? Extensions { get; set; }
    }

    public static class TestServerFactory
    {
        public static IWebHost CreateServer(Action<IServiceCollection> configure, out int port)
        {
            for (port = 5500; port < 6000; port++)
            {
                try
                {
                    var configBuilder = new ConfigurationBuilder();
                    configBuilder.AddInMemoryCollection();
                    IConfigurationRoot config = configBuilder.Build();
                    config["server.urls"] = $"http://localhost:{port}";
                    IWebHost host = new WebHostBuilder()
                        .UseConfiguration(config)
                        .UseKestrel()
                        .ConfigureServices(services =>
                        {
                            services.AddHttpContextAccessor();
                            services.AddRouting();
                            configure(services);
                        })
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseEndpoints(c => c.MapGraphQL());
                        })
                        .Build();

                    host.Start();

                    return host;
                }
                catch
                {
                    // ignored
                }
            }

            throw new InvalidOperationException("Not port found");
        }
    }
}
