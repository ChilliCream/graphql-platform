using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Client;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Stitching.Resources;
using HotChocolate.Stitching.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Stitching
{
    public class StitchingBuilder
        : IStitchingBuilder
    {
        private OrderedDictionary<NameString, LoadSchemaDocument> _schemas =
            new OrderedDictionary<NameString, LoadSchemaDocument>();
        private readonly List<LoadSchemaDocument> _extensions =
            new List<LoadSchemaDocument>();
        private readonly List<MergeTypeHandler> _mergeHandlers =
            new List<MergeTypeHandler>();

        public IStitchingBuilder AddSchema(
            NameString name,
            LoadSchemaDocument loadSchema)
        {
            if (loadSchema == null)
            {
                throw new ArgumentNullException(nameof(loadSchema));
            }

            name.EnsureNotEmpty(nameof(name));

            _schemas.Add(name, loadSchema);

            return this;
        }

        public IStitchingBuilder AddExtensions(
            LoadSchemaDocument loadExtensions)
        {
            if (loadExtensions == null)
            {
                throw new ArgumentNullException(nameof(loadExtensions));
            }

            _extensions.Add(loadExtensions);

            return this;
        }

        public IStitchingBuilder AddMergeHandler(MergeTypeHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _mergeHandlers.Add(handler);

            return this;
        }

        public void Populate(
            IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            serviceCollection.TryAddSingleton(services =>
                StitchingFactory.Create(
                    this, services, configure, options));

            serviceCollection.TryAddScoped(services =>
                services.GetRequiredService<StitchingFactory>()
                    .CreateStitchingContext(services));

            if (!serviceCollection.Any(d =>
                d.ImplementationType == typeof(RemoteQueryBatchOperation)))
            {
                serviceCollection.AddScoped<
                    IBatchOperation,
                    RemoteQueryBatchOperation>();
            }

            serviceCollection.TryAddSingleton(services =>
                services.GetRequiredService<StitchingFactory>()
                    .CreateStitchedQueryExecuter());

            serviceCollection.TryAddSingleton(services =>
                services.GetRequiredService<IQueryExecutor>()
                    .Schema);
        }

        public static StitchingBuilder New() => new StitchingBuilder();

        private class StitchingFactory
        {
            private readonly IReadOnlyList<IRemoteExecutorAccessor> _executors;
            private readonly DocumentNode _mergedSchema;
            private readonly Action<ISchemaConfiguration> _configure;
            private readonly IQueryExecutionOptionsAccessor _options;

            private StitchingFactory(
                IReadOnlyList<IRemoteExecutorAccessor> executors,
                DocumentNode mergedSchema,
                Action<ISchemaConfiguration> configure,
                IQueryExecutionOptionsAccessor options)
            {
                _executors = executors;
                _mergedSchema = mergedSchema;
                _configure = configure;
                _options = options;
            }

            public IStitchingContext CreateStitchingContext(
                IServiceProvider services)
            {
                return new StitchingContext(services, _executors);
            }

            public IQueryExecutor CreateStitchedQueryExecuter()
            {
                return Schema.Create(
                    _mergedSchema,
                    c =>
                    {
                        _configure(c);
                        c.RegisterExtendedScalarTypes();
                        c.UseSchemaStitching();
                    })
                    .MakeExecutable(b => b.UseStitchingPipeline(_options));
            }

            public static StitchingFactory Create(
                StitchingBuilder builder,
                IServiceProvider services,
                Action<ISchemaConfiguration> configure,
                IQueryExecutionOptionsAccessor options)
            {
                IDictionary<NameString, DocumentNode> schemas =
                    LoadSchemas(builder._schemas, services);
                IReadOnlyList<DocumentNode> extensions =
                    LoadExtensions(builder._extensions, services);
                IReadOnlyList<IRemoteExecutorAccessor> executors =
                    CreateRemoteExecutors(schemas);
                DocumentNode mergedSchema =
                    MergeSchemas(builder, schemas, extensions);

                return new StitchingFactory(
                    executors, mergedSchema,
                    configure, options);
            }

            private static IDictionary<NameString, DocumentNode> LoadSchemas(
                IDictionary<NameString, LoadSchemaDocument> schemaLoaders,
                IServiceProvider services)
            {
                var schemas = new OrderedDictionary<NameString, DocumentNode>();

                foreach (NameString name in schemaLoaders.Keys)
                {
                    schemas[name] = schemaLoaders[name].Invoke(services);
                }

                return schemas;
            }

            private static IReadOnlyList<DocumentNode> LoadExtensions(
                IReadOnlyList<LoadSchemaDocument> extensionsLoaders,
                IServiceProvider services)
            {
                var extensions = new List<DocumentNode>();

                foreach (LoadSchemaDocument loadExtensions in extensionsLoaders)
                {
                    extensions.Add(loadExtensions.Invoke(services));
                }

                return extensions;
            }

            private static IReadOnlyList<IRemoteExecutorAccessor> CreateRemoteExecutors(
               IDictionary<NameString, DocumentNode> schemas)
            {
                var executors = new List<IRemoteExecutorAccessor>();

                foreach (NameString name in schemas.Keys)
                {
                    IQueryExecutor executor = Schema.Create(schemas[name], c =>
                    {
                        c.UseNullResolver();
                        c.RegisterExtendedScalarTypes();
                    }).MakeExecutable(b => b.UseQueryDelegationPipeline(name));
                    executors.Add(new RemoteExecutorAccessor(name, executor));
                }

                return executors;
            }

            private static DocumentNode MergeSchemas(
                StitchingBuilder builder,
                IDictionary<NameString, DocumentNode> schemas,
                IReadOnlyList<DocumentNode> extensions)
            {
                var merger = new SchemaMerger();

                foreach (NameString name in schemas.Keys)
                {
                    merger.AddSchema(name, schemas[name]);
                }

                foreach (DocumentNode extension in extensions)
                {
                    // add support for extensions
                }

                foreach (MergeTypeHandler handler in builder._mergeHandlers)
                {
                    merger.AddMergeHandler(handler);
                }

                return merger.Merge();
            }
        }
    }

    public static class StitchingBuilderExtensions
    {
        public static IStitchingBuilder AddSchemaFromFile(
            this IStitchingBuilder builder,
            NameString name,
            string path)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(
                    "The schema file path mustn't be null or empty,",
                    nameof(path));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s =>
                Parser.Default.Parse(
                    File.ReadAllText(path)));
            return builder;
        }

        public static IStitchingBuilder AddSchemaFromHttp(
            this IStitchingBuilder builder,
            NameString name)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            name.EnsureNotEmpty(nameof(name));

            builder.AddSchema(name, s =>
            {
                HttpClient httpClient = s.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(name);

                string introspectionQuery = EmbeddedResources
                    .OpenText("IntrospectionQuery.graphql");

                var request = new RemoteQueryRequest
                {
                    Query = introspectionQuery
                };

                var queryClient = new HttpQueryClient();
                string result = Task.Factory.StartNew(
                    () => queryClient.FetchStringAsync(request, httpClient))
                    .Unwrap().GetAwaiter().GetResult();
                return IntrospectionDeserializer.Deserialize(result);
            });

            return builder;
        }
    }
}
