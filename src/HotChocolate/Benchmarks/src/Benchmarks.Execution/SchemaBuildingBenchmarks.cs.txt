using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.ConferencePlanner.Attendees;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.ConferencePlanner.DataLoader;
using HotChocolate.ConferencePlanner.Imports;
using HotChocolate.ConferencePlanner.Sessions;
using HotChocolate.ConferencePlanner.Speakers;
using HotChocolate.ConferencePlanner.Tracks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class SchemaBuildingBenchmarks
{
    [Benchmark]
    public async Task CreateSchema_AnnotationBased_Medium()
    {
        // for (int i = 0; i < 20; i++)
        {
            var servicesCollection = new ServiceCollection();
            await ConfigureAnnotationBased(servicesCollection).BuildSchemaAsync();
        }
    }

    [Benchmark]
    public async Task CreateSchema_SchemaFirst_Medium()
    {
        for (int i = 0; i < 20; i++)
        {
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(SchemaMedium)
                .AddType(new StringType())
                .AddType(new StringType("Scalar1"))
                .AddType(new StringType("Scalar2"))
                .AddType(new StringType("Scalar3"))
                .AddType(new StringType("Scalar4"))
                .UseField(_ => _ => default)
                .BuildSchemaAsync();
        }
    }

    public string SchemaMedium { get; } = Resources.SchemaMedium;

    [Benchmark]
    public async Task CreateSchema_SchemaFirst_Large()
    {
        // for (int i = 0; i < 20; i++)
        {
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(SchemaLarge)
                .AddType(new StringType())
                .AddType(new StringType("Scalar1"))
                .AddType(new StringType("Scalar2"))
                .AddType(new StringType("Scalar3"))
                .AddType(new StringType("Scalar4"))
                .AddType(new StringType("Scalar5"))
                .AddType(new StringType("Scalar6"))
                .UseField(_ => _ => default)
                .BuildSchemaAsync();
        }
    }

    public string SchemaLarge { get; } = Resources.SchemaLarge;

    private static IRequestExecutorBuilder ConfigureAnnotationBased(IServiceCollection services)
    {
        return services
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

}