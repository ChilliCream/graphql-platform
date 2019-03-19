using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Stitching.Client;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Properties;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public partial class StitchingBuilder
        : IStitchingBuilder
    {
        private readonly OrderedDictionary<NameString, LoadSchemaDocument>
            _schemas = new OrderedDictionary<NameString, LoadSchemaDocument>();
        private readonly OrderedDictionary<NameString, ExecutorFactory>
            _execFacs = new OrderedDictionary<NameString, ExecutorFactory>();
        private readonly List<LoadSchemaDocument> _extensions =
            new List<LoadSchemaDocument>();
        private readonly List<MergeTypeRuleFactory> _mergeRules =
            new List<MergeTypeRuleFactory>();
        private readonly List<Action<ISchemaConfiguration>> _schemaConfigs =
            new List<Action<ISchemaConfiguration>>();
        private readonly List<Action<IQueryExecutionBuilder>> _execConfigs =
            new List<Action<IQueryExecutionBuilder>>();
        private readonly List<ITypeRewriter> _typeRewriters =
            new List<ITypeRewriter>();
        private readonly List<IDocumentRewriter> _docRewriters =
            new List<IDocumentRewriter>();
        private readonly List<Func<DocumentNode, DocumentNode>> _mergedDocRws =
            new List<Func<DocumentNode, DocumentNode>>();
        private readonly List<Action<DocumentNode>> _mergedDocVis =
            new List<Action<DocumentNode>>();

        private IQueryExecutionOptionsAccessor _options;

        public IStitchingBuilder AddSchema(
            NameString name,
            LoadSchemaDocument loadSchema)
        {
            if (loadSchema == null)
            {
                throw new ArgumentNullException(nameof(loadSchema));
            }

            name.EnsureNotEmpty(nameof(name));

            if (_schemas.ContainsKey(name) || _execFacs.ContainsKey(name))
            {
                throw new ArgumentException(
                    StitchingResources.StitchingBuilder_SchemaNameInUse,
                    nameof(name));
            }

            _schemas.Add(name, loadSchema);

            return this;
        }

        public IStitchingBuilder AddQueryExecutor(
            NameString name,
            ExecutorFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            name.EnsureNotEmpty(nameof(name));

            if (_schemas.ContainsKey(name) || _execFacs.ContainsKey(name))
            {
                throw new ArgumentException(
                    StitchingResources.StitchingBuilder_SchemaNameInUse,
                    nameof(name));
            }

            _execFacs.Add(name, factory);

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

        public IStitchingBuilder AddMergeRule(MergeTypeRuleFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _mergeRules.Add(factory);

            return this;
        }

        public IStitchingBuilder AddSchemaConfiguration(
            Action<ISchemaConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _schemaConfigs.Add(configure);
            return this;
        }

        public IStitchingBuilder AddExecutionConfiguration(
            Action<IQueryExecutionBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            _execConfigs.Add(configure);
            return this;
        }

        public IStitchingBuilder SetExecutionOptions(
            IQueryExecutionOptionsAccessor options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options;
            return this;
        }

        public IStitchingBuilder AddTypeRewriter(ITypeRewriter rewriter)
        {
            if (rewriter == null)
            {
                throw new ArgumentNullException(nameof(rewriter));
            }

            _typeRewriters.Add(rewriter);
            return this;
        }

        public IStitchingBuilder AddDocumentRewriter(IDocumentRewriter rewriter)
        {
            if (rewriter == null)
            {
                throw new ArgumentNullException(nameof(rewriter));
            }

            _docRewriters.Add(rewriter);
            return this;
        }

        public IStitchingBuilder AddMergedDocumentRewriter(
            Func<DocumentNode, DocumentNode> rewrite)
        {
            if (rewrite == null)
            {
                throw new ArgumentNullException(nameof(rewrite));
            }

            _mergedDocRws.Add(rewrite);
            return this;
        }

        public IStitchingBuilder AddMergedDocumentVisitor(
            Action<DocumentNode> visit)
        {
            if (visit == null)
            {
                throw new ArgumentNullException(nameof(visit));
            }

            _mergedDocVis.Add(visit);
            return this;
        }

        public void Populate(IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.TryAddSingleton<
                IQueryResultSerializer,
                JsonQueryResultSerializer>();

            foreach (KeyValuePair<NameString, ExecutorFactory> factory in
                _execFacs)
            {
                serviceCollection.AddSingleton<IRemoteExecutorAccessor>(
                    services => new RemoteExecutorAccessor(
                            factory.Key,
                            factory.Value.Invoke(services)));
            }

            serviceCollection.TryAddSingleton(services =>
                    StitchingFactory.Create(this, services));

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

            serviceCollection.TryAddSingleton<IQueryExecutor>(
                services => new LazyQueryExecutor(() =>
                    services.GetRequiredService<StitchingFactory>()
                        .CreateStitchedQueryExecuter(services)));

            serviceCollection.TryAddSingleton(services =>
                services.GetRequiredService<IQueryExecutor>()
                    .Schema);
        }

        public static StitchingBuilder New() => new StitchingBuilder();
    }
}
