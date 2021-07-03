using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate.ConferencePlanner.Attendees;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate.ConferencePlanner.Imports;
using HotChocolate.ConferencePlanner.Sessions;
using HotChocolate.ConferencePlanner.Speakers;
using HotChocolate.ConferencePlanner.Tracks;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ConferencePlanner
{
    public class BenchmarkBase
    {
        private static readonly MD5DocumentHashProvider _md5 = new MD5DocumentHashProvider();

        public BenchmarkBase()
        {
            var servicesCollection = new ServiceCollection();
            ConfigureServices(servicesCollection);
            Services = servicesCollection.BuildServiceProvider();
            ExecutorResolver = Services.GetRequiredService<IRequestExecutorResolver>();
        }

        public IServiceProvider Services { get; }

        public IRequestExecutorResolver ExecutorResolver { get; }

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

        public Task BenchmarkAsync(string requestDocument)
        {
            return BenchmarkAsync(new QueryRequest(new QuerySourceText(requestDocument)));
        }

        public async Task BenchmarkAsync(IQueryRequest request)
        {
            IRequestExecutor executor = await ExecutorResolver.GetRequestExecutorAsync();
            IExecutionResult result = await executor.ExecuteAsync(request);

            if (result.Errors is { Count: > 0 })
            {
                throw new InvalidOperationException("The request failed.");
            }

            if (result is IDisposable d)
            {
                // d.Dispose();
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
                    executor.Schema.GetOperationType(operation.Operation));

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
                    .EnableRelaySupport()

                    // Now we add some the DataLoader to our system. 
                    .AddDataLoader<AttendeeByIdDataLoader>()
                    .AddDataLoader<SessionByIdDataLoader>()
                    .AddDataLoader<SpeakerByIdDataLoader>()
                    .AddDataLoader<TrackByIdDataLoader>()

                    // .AddDiagnosticEventListener<BatchDiagnostics>()

                    // we make sure that the db exists and prefill it with conference data.
                    .EnsureDatabaseIsCreated()

                    // Since we are using subscriptions, we need to register a pub/sub system.
                    // for our demo we are using a in-memory pub/sub system.
                    .AddInMemorySubscriptions();

        }
    }

    public class BatchDiagnostics : DiagnosticEventListener
    {
        public override IActivityScope ExecuteRequest(IRequestContext context)
        {
            var scope = new RequestScope();
            context.ContextData[nameof(RequestScope)] = scope;
            return scope;
        }

        public override IActivityScope DispatchBatch(
            IRequestContext context)
        {
            return new BatchScope(((RequestScope)context.ContextData[nameof(RequestScope)]!));
        }

        public override void ScaleTaskProcessorsUp(
            IRequestContext context,
            int backlogSize,
            int processors)
        {
            TimeSpan timeSpan = ((RequestScope)context.ContextData[nameof(RequestScope)]!).Elapsed;
            Console.WriteLine($"{timeSpan} Scaled up to {processors} processors with backlog size {backlogSize}.");
        }

        public override void ScaleTaskProcessorsDown(
            IRequestContext context,
            int backlogSize,
            int processors)
        {
            TimeSpan timeSpan = ((RequestScope)context.ContextData[nameof(RequestScope)]!).Elapsed;
            Console.WriteLine($"{timeSpan} Scaled down to {processors} processors with backlog size {backlogSize}.");
        }

        private class RequestScope : IActivityScope
        {
            private readonly Stopwatch _stopwatch;

            public RequestScope()
            {
                _stopwatch = Stopwatch.StartNew();
                Console.WriteLine($"{DateTime.Now} Execute Request.");
            }

            public TimeSpan Elapsed => _stopwatch.Elapsed;

            public void Dispose()
            {
                Console.WriteLine($"Completed in {Elapsed}");
                _stopwatch.Stop();
            }
        }

        private class BatchScope : IActivityScope
        {
            private RequestScope _requestScope;

            public BatchScope(RequestScope requestScope)
            {
                _requestScope = requestScope;
                Console.WriteLine($"{_requestScope.Elapsed} Begin Dispatching Batch.");
            }

            public void Dispose()
            {
                Console.WriteLine($"{_requestScope.Elapsed} End Dispatching Batch.");
            }
        }
    }
}
