using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Stitching.Client;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Properties;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Stitching.Merge.Rewriters;

namespace HotChocolate.Stitching
{
    public partial class StitchingBuilder
    {
        private class StitchingFactory
        {
            private readonly StitchingBuilder _builder;
            private readonly IReadOnlyList<IRemoteExecutorAccessor> _executors;
            private readonly DocumentNode _mergedSchema;
            private readonly IQueryExecutionOptionsAccessor _options;

            private StitchingFactory(
                StitchingBuilder builder,
                IReadOnlyList<IRemoteExecutorAccessor> executors,
                DocumentNode mergedSchema)
            {
                _builder = builder;
                _executors = executors;
                _mergedSchema = mergedSchema;
                _options = _builder._options ?? new QueryExecutionOptions();
            }

            public IStitchingContext CreateStitchingContext(
                IServiceProvider services)
            {
                return new StitchingContext(services, _executors);
            }

            public IQueryExecutor CreateStitchedQueryExecuter()
            {
                return new LazyQueryExecutor(() =>
                    Schema.Create(
                        _mergedSchema,
                        c =>
                        {
                            foreach (Action<ISchemaConfiguration> configure in
                                _builder._schemaConfigs)
                            {
                                configure(c);
                            }
                            c.RegisterExtendedScalarTypes();
                            c.UseSchemaStitching();
                        })
                        .MakeExecutable(b =>
                        {
                            foreach (Action<IQueryExecutionBuilder> configure in
                                _builder._execConfigs)
                            {
                                configure(b);
                            }
                            return b.UseStitchingPipeline(_options);
                        }));
            }

            public static StitchingFactory Create(
                StitchingBuilder builder,
                IServiceProvider services)
            {
                IDictionary<NameString, DocumentNode> schemas =
                    LoadSchemas(builder._schemas, services);
                IReadOnlyList<DocumentNode> extensions =
                    LoadExtensions(builder._extensions, services);
                IReadOnlyList<IRemoteExecutorAccessor> executors =
                    CreateRemoteExecutors(schemas);
                DocumentNode mergedSchema =
                    MergeSchemas(builder, schemas);
                mergedSchema = AddExtensions(mergedSchema, extensions);

                return new StitchingFactory(builder, executors, mergedSchema);
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

            private static IReadOnlyList<IRemoteExecutorAccessor>
                CreateRemoteExecutors(
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
                IDictionary<NameString, DocumentNode> schemas)
            {
                var merger = new SchemaMerger();

                foreach (NameString name in schemas.Keys)
                {
                    merger.AddSchema(name, schemas[name]);
                }

                foreach (MergeTypeRuleFactory handler in builder._mergeRules)
                {
                    merger.AddMergeRule(handler);
                }

                foreach (IDocumentRewriter rewriter in builder._docRewriters)
                {
                    merger.AddDocumentRewriter(rewriter);
                }

                foreach (ITypeRewriter rewriter in builder._typeRewriters)
                {
                    merger.AddTypeRewriter(rewriter);
                }

                return merger.Merge();
            }

            private static DocumentNode AddExtensions(
                DocumentNode schema,
                IReadOnlyList<DocumentNode> extensions)
            {
                if (extensions.Count == 0)
                {
                    return schema;
                }

                var rewriter = new AddSchemaExtensionRewriter();
                DocumentNode currentSchema = schema;

                foreach (DocumentNode extension in extensions)
                {
                    currentSchema = rewriter.AddExtensions(
                        currentSchema, extension);
                }

                return currentSchema;
            }
        }
    }
}
