using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Delegation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching
{
    public class StitchingBuilder
        : IStitchingBuilder
    {
        private OrderedDictionary<NameString, Func<DocumentNode>> _schemas =
            new OrderedDictionary<NameString, Func<DocumentNode>>();
        private readonly List<Func<DocumentNode>> _extensions =
            new List<Func<DocumentNode>>();
        private readonly List<MergeTypeHandler> _mergeHandlers =
            new List<MergeTypeHandler>();

        public IStitchingBuilder AddSchema(
            NameString name,
            Func<DocumentNode> loadSchema)
        {
            if (loadSchema == null)
            {
                throw new ArgumentNullException(nameof(loadSchema));
            }

            name.EnsureNotEmpty(nameof(name));

            _schemas.Add(name, loadSchema);

            return this;
        }

        public IStitchingBuilder AddExtensions(Func<DocumentNode> loadExtensions)
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

        public void Populate(IServiceCollection services,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var merger = new SchemaMerger();

            AddRemoteExecuters(services, merger);

            DocumentNode mergedSchema = merger.Merge();

            services.AddSingleton<
                IQueryResultSerializer,
                JsonQueryResultSerializer>();

            IQueryExecutor stitchedExecutor = Schema.Create(
                mergedSchema,
                c =>
                {
                    configure(c);
                    c.RegisterExtendedScalarTypes();
                    c.UseSchemaStitching();
                })
                .MakeExecutable(b => b.UseStitchingPipeline(options));

            services.AddSingleton(stitchedExecutor)
                .AddSingleton(stitchedExecutor.Schema);
        }

        private void AddRemoteExecuters(
            IServiceCollection services,
            SchemaMerger merger)
        {
            var executors = new List<IRemoteExecutorAccessor>();

            foreach (NameString name in _schemas.Keys)
            {
                DocumentNode schemaDocument = _schemas[name].Invoke();
                IQueryExecutor executor = Schema.Create(schemaDocument, c =>
                {
                    c.RegisterExtendedScalarTypes();
                }).MakeExecutable(b => b.UseQueryDelegationPipeline(name));
                executors.Add(new RemoteExecutorAccessor(name, executor));
                merger.AddSchema(name, _schemas[name].Invoke());
            }

            foreach (IRemoteExecutorAccessor executor in executors)
            {
                services.AddRemoteQueryExecutor(executor);
            }

            foreach (MergeTypeHandler handler in _mergeHandlers)
            {
                merger.AddMergeHandler(handler);
            }
        }
    }

    /*

    StichingBuilder.New()
    .AddRemoteSchema(foo)
    .AddExtensionFromFile("Extensions.graphql")
    .AddConflictResolver(types => new ConflictResolution(false))
    .AddFieldSelector((type, field) => false)
    .RenameField("schemaName", new FieldReference("Type", "field"), "newFieldName")
    .Merge();
     */
}
