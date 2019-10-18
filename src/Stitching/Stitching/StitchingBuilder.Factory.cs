using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Client;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Stitching.Merge.Rewriters;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Configuration;
using HotChocolate.Types.Introspection;
using HotChocolate.Stitching.Introspection;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public partial class StitchingBuilder
    {
        internal class StitchingFactory
        {
            private readonly StitchingBuilder _builder;
            private readonly IReadOnlyList<IRemoteExecutorAccessor> _executors;

            private StitchingFactory(
                StitchingBuilder builder,
                IReadOnlyList<IRemoteExecutorAccessor> executors,
                DocumentNode mergedSchema)
            {
                _builder = builder;
                _executors = executors;
                MergedSchema = mergedSchema;
            }

            public DocumentNode MergedSchema { get; }

            public IStitchingContext CreateStitchingContext(
                IServiceProvider services)
            {
                return new StitchingContext(services, _executors);
            }

            public ISchema CreateStitchedSchema(
                IServiceProvider serviceProvider)
            {
                return Schema.Create(
                    MergedSchema,
                    c =>
                    {
                        foreach (Action<ISchemaConfiguration> configure in
                            _builder._schemaConfigs)
                        {
                            configure(c);
                        }
                        c.RegisterExtendedScalarTypes();
                        c.UseSchemaStitching();
                        c.RegisterServiceProvider(serviceProvider);
                    });
            }

            public static StitchingFactory Create(
                StitchingBuilder builder,
                IServiceProvider services)
            {
                // fetch schemas for createing remote schemas
                IDictionary<NameString, DocumentNode> remoteSchemas =
                    LoadSchemas(builder._schemas, services);

                // fetch schema extensions
                IReadOnlyList<DocumentNode> extensions =
                    LoadExtensions(builder._extensions, services);

                // add local remote executors
                var executors = new List<IRemoteExecutorAccessor>(
                   services.GetServices<IRemoteExecutorAccessor>());

                // create schema map for merge process
                var allSchemas = new Dictionary<NameString, DocumentNode>(
                    remoteSchemas);

                // add schemas from local remote schemas for merging them
                AddSchemasFromExecutors(allSchemas, executors);

                // add remote executors
                executors.AddRange(CreateRemoteExecutors(remoteSchemas));

                // merge schema
                DocumentNode mergedSchema = MergeSchemas(builder, allSchemas);
                mergedSchema = AddExtensions(mergedSchema, extensions);
                mergedSchema = RewriteMerged(builder, mergedSchema);
                mergedSchema = IntrospectionClient.RemoveBuiltInTypes(mergedSchema);

                VisitMerged(builder, mergedSchema);

                // create factory
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

            private static void AddSchemasFromExecutors(
                IDictionary<NameString, DocumentNode> schemas,
                IEnumerable<IRemoteExecutorAccessor> accessors)
            {
                foreach (IRemoteExecutorAccessor accessor in accessors)
                {
                    schemas[accessor.SchemaName] =
                        SchemaSerializer.SerializeSchema(
                            accessor.Executor.Schema);
                }
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
                    DocumentNode schema =
                        IntrospectionClient.RemoveBuiltInTypes(schemas[name]);

                    IQueryExecutor executor = Schema.Create(schema, c =>
                    {
                        c.Options.StrictValidation = false;

                        c.UseNullResolver();

                        foreach (ScalarTypeDefinitionNode typeDefinition in
                            schema.Definitions.OfType<ScalarTypeDefinitionNode>())
                        {
                            c.RegisterType(new StringType(
                                typeDefinition.Name.Value,
                                typeDefinition.Description?.Value));
                        }
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
                    merger.AddTypeMergeRule(handler);
                }

                foreach (MergeDirectiveRuleFactory handler in
                    builder._mergeDirectiveRules)
                {
                    merger.AddDirectiveMergeRule(handler);
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
                IReadOnlyCollection<DocumentNode> extensions)
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

            private static DocumentNode RewriteMerged(
                StitchingBuilder builder,
                DocumentNode schema)
            {
                if (builder._mergedDocRws.Count == 0)
                {
                    return schema;
                }

                DocumentNode current = schema;

                foreach (Func<DocumentNode, DocumentNode> rewriter in
                    builder._mergedDocRws)
                {
                    current = rewriter.Invoke(current);
                }

                return current;
            }

            private static void VisitMerged(
                StitchingBuilder builder,
                DocumentNode schema)
            {
                if (builder._mergedDocVis.Count > 0)
                {
                    foreach (Action<DocumentNode> visitor in
                        builder._mergedDocVis)
                    {
                        visitor.Invoke(schema);
                    }
                }
            }
        }
    }
}
