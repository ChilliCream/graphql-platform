using System;
using System.Linq;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Execution;
using HotChocolate.Stitching.Merge.Handlers;
using HotChocolate.Stitching.Merge.Rewriters;

namespace HotChocolate.Stitching.Merge
{
    public class SchemaMerger
        : ISchemaMerger
    {
        private static readonly List<MergeTypeRuleFactory> _defaultMergeRules =
            new List<MergeTypeRuleFactory>
            {
                SchemaMergerExtensions.CreateTypeMergeRule<ScalarTypeMergeHandler>(),
                SchemaMergerExtensions.CreateTypeMergeRule<InputObjectTypeMergeHandler>(),
                SchemaMergerExtensions.CreateTypeMergeRule<RootTypeMergeHandler>(),
                SchemaMergerExtensions.CreateTypeMergeRule<ObjectTypeMergeHandler>(),
                SchemaMergerExtensions.CreateTypeMergeRule<InterfaceTypeMergeHandler>(),
                SchemaMergerExtensions.CreateTypeMergeRule<UnionTypeMergeHandler>(),
                SchemaMergerExtensions.CreateTypeMergeRule<EnumTypeMergeHandler>(),
            };
        private readonly List<MergeTypeRuleFactory> _mergeRules =
            new List<MergeTypeRuleFactory>();
        private readonly List<MergeDirectiveRuleFactory> _directiveMergeRules =
            new List<MergeDirectiveRuleFactory>();
        private readonly List<ITypeRewriter> _typeRewriters =
            new List<ITypeRewriter>();
        private readonly List<IDocumentRewriter> _docRewriters =
            new List<IDocumentRewriter>();
        private readonly OrderedDictionary<NameString, DocumentNode> _schemas =
            new OrderedDictionary<NameString, DocumentNode>();

        [Obsolete("Use AddTypeMergeRule")]
        public ISchemaMerger AddMergeRule(MergeTypeRuleFactory factory) =>
            AddTypeMergeRule(factory);

        public ISchemaMerger AddTypeMergeRule(MergeTypeRuleFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _mergeRules.Add(factory);
            return this;
        }

        public ISchemaMerger AddDirectiveMergeRule(
            MergeDirectiveRuleFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _directiveMergeRules.Add(factory);
            return this;
        }

        public ISchemaMerger AddSchema(NameString name, DocumentNode schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            name.EnsureNotEmpty(nameof(name));

            _schemas.Add(name, schema);

            return this;
        }

        public ISchemaMerger AddTypeRewriter(ITypeRewriter rewriter)
        {
            if (rewriter == null)
            {
                throw new ArgumentNullException(nameof(rewriter));
            }

            _typeRewriters.Add(rewriter);
            return this;
        }

        public ISchemaMerger AddDocumentRewriter(IDocumentRewriter rewriter)
        {
            if (rewriter == null)
            {
                throw new ArgumentNullException(nameof(rewriter));
            }

            _docRewriters.Add(rewriter);
            return this;
        }

        public DocumentNode Merge()
        {
            MergeTypeRuleDelegate mergeTypes = CompileMergeTypeDelegate();
            MergeDirectiveRuleDelegate mergeDirectives = CompileMergeDirectiveDelegate();
            IReadOnlyList<ISchemaInfo> schemas = CreateSchemaInfos();

            var context = new SchemaMergeContext();

            // merge root types
            MergeRootType(context, OperationType.Query, schemas, mergeTypes);
            MergeRootType(context, OperationType.Mutation, schemas, mergeTypes);
            MergeRootType(context, OperationType.Subscription, schemas, mergeTypes);

            // merge all other types
            MergeTypes(context, CreateTypesNameSet(schemas), schemas, mergeTypes);
            MergeDirectives(context, CreateDirectivesNameSet(schemas), schemas, mergeDirectives);

            return RewriteTypeReferences(schemas, context.CreateSchema());
        }

        private IReadOnlyList<ISchemaInfo> CreateSchemaInfos()
        {
            List<SchemaInfo> original = _schemas
                .Select(t => new SchemaInfo(t.Key, PrepareSchemaDocument(t.Value, t.Key)))
                .ToList();

            if (_docRewriters.Count == 0 && _typeRewriters.Count == 0)
            {
                return original;
            }

            var rewritten = new List<SchemaInfo>();
            var referenceRewriter = new TypeReferenceRewriter();

            foreach (SchemaInfo schemaInfo in original)
            {
                DocumentNode current = schemaInfo.Document;
                current = RewriteDocument(schemaInfo, current);
                current = RewriteTypes(schemaInfo, current);

                if (current == schemaInfo.Document)
                {
                    rewritten.Add(schemaInfo);
                }
                else
                {
                    current = referenceRewriter.RewriteSchema(
                        current, schemaInfo.Name);

                    rewritten.Add(new SchemaInfo(
                        schemaInfo.Name,
                        current));
                }
            }

            return rewritten;
        }

        private static DocumentNode PrepareSchemaDocument(
            DocumentNode document,
            NameString schemaName)
        {
            var definitions = new List<IDefinitionNode>();
            foreach (IDefinitionNode definition in document.Definitions)
            {
                if (definition is ITypeDefinitionNode typeDefinition)
                {
                    if (!IsIntrospectionType(typeDefinition))
                    {
                        definitions.Add(typeDefinition.Rename(
                            typeDefinition.Name.Value, schemaName));
                    }
                }
                else
                {
                    definitions.Add(definition);
                }
            }
            return document.WithDefinitions(definitions);
        }

        private static bool IsIntrospectionType(ITypeDefinitionNode typeDefinition)
        {
            // we should check this against the actual known list of intro types.
            return typeDefinition.Name.Value.StartsWith("__", StringComparison.Ordinal);
        }

        private DocumentNode RewriteDocument(
            ISchemaInfo schema,
            DocumentNode document)
        {
            DocumentNode current = document;

            foreach (IDocumentRewriter rewriter in _docRewriters)
            {
                current = rewriter.Rewrite(schema, current);
            }

            return current;
        }

        private DocumentNode RewriteTypes(
            ISchemaInfo schema,
            DocumentNode document)
        {
            if (_typeRewriters.Count == 0)
            {
                return document;
            }

            var definitions = new List<IDefinitionNode>();

            foreach (IDefinitionNode definition in document.Definitions)
            {
                if (definition is ITypeDefinitionNode typeDefinition)
                {
                    foreach (ITypeRewriter rewriter in _typeRewriters)
                    {
                        typeDefinition = rewriter.Rewrite(schema, typeDefinition);
                    }
                    definitions.Add(typeDefinition);
                }
                else
                {
                    definitions.Add(definition);
                }
            }

            return document.WithDefinitions(definitions);
        }

        private static DocumentNode RewriteTypeReferences(
            IReadOnlyList<ISchemaInfo> schemas,
            DocumentNode document)
        {
            DocumentNode current = document;
            var referenceRewriter = new TypeReferenceRewriter();

            foreach (ISchemaInfo schema in schemas)
            {
                current = referenceRewriter.RewriteSchema(current, schema.Name);
            }

            return current;
        }

        private static void MergeRootType(
            ISchemaMergeContext context,
            OperationType operation,
            IEnumerable<ISchemaInfo> schemas,
            MergeTypeRuleDelegate merge)
        {
            var types = new List<TypeInfo>();

            foreach (ISchemaInfo schema in schemas)
            {
                ObjectTypeDefinitionNode rootType = schema.GetRootType(operation);
                if (rootType is not null)
                {
                    types.Add(new ObjectTypeInfo(rootType, schema));
                }
            }

            if (types.Count > 0)
            {
                merge(context, types);
            }
        }

        private void MergeTypes(
            ISchemaMergeContext context,
            ISet<string> typeNames,
            IReadOnlyCollection<ISchemaInfo> schemas,
            MergeTypeRuleDelegate merge)
        {
            var types = new List<ITypeInfo>();

            foreach (string typeName in typeNames)
            {
                SetTypes(typeName, schemas, types);
                merge(context, types);
            }
        }

        private static ISet<string> CreateTypesNameSet(
            IReadOnlyCollection<ISchemaInfo> schemas)
        {
            HashSet<string> names = new HashSet<string>();

            foreach (ISchemaInfo schema in schemas)
            {
                foreach (string name in schema.Types.Keys)
                {
                    names.Add(name);
                }
            }

            return names;
        }

        private static ISet<string> CreateDirectivesNameSet(
            IReadOnlyCollection<ISchemaInfo> schemas)
        {
            HashSet<string> names = new HashSet<string>();

            foreach (ISchemaInfo schema in schemas)
            {
                foreach (string name in schema.Directives.Keys)
                {
                    names.Add(name);
                }
            }

            return names;
        }

        private void MergeDirectives(
            ISchemaMergeContext context,
            ISet<string> typeNames,
            IReadOnlyCollection<ISchemaInfo> schemas,
            MergeDirectiveRuleDelegate merge)
        {
            var directives = new List<IDirectiveTypeInfo>();

            foreach (string typeName in typeNames)
            {
                SetDirectives(typeName, schemas, directives);
                merge(context, directives);
            }
        }

        private void SetTypes(
            string name,
            IReadOnlyCollection<ISchemaInfo> schemas,
            ICollection<ITypeInfo> types)
        {
            types.Clear();

            foreach (ISchemaInfo schema in schemas)
            {
                if (schema.Types.TryGetValue(name,
                    out ITypeDefinitionNode typeDefinition))
                {
                    types.Add(TypeInfo.Create(typeDefinition, schema));
                }
            }
        }

        private void SetDirectives(
            string name,
            IReadOnlyCollection<ISchemaInfo> schemas,
            ICollection<IDirectiveTypeInfo> directives)
        {
            directives.Clear();

            foreach (ISchemaInfo schema in schemas)
            {
                if (schema.Directives.TryGetValue(name,
                    out DirectiveDefinitionNode directiveDefinition))
                {
                    directives.Add(new DirectiveTypeInfo(
                        directiveDefinition, schema));
                }
            }
        }

        private MergeTypeRuleDelegate CompileMergeTypeDelegate()
        {
            MergeTypeRuleDelegate current = (c, t) =>
            {
                if (t.Count > 0)
                {
                    throw new NotSupportedException(
                        "The type definitions could not be handled.");
                }
            };

            var handlers = new List<MergeTypeRuleFactory>();
            handlers.AddRange(_mergeRules);
            handlers.AddRange(_defaultMergeRules);

            for (var i = handlers.Count - 1; i >= 0; i--)
            {
                current = handlers[i].Invoke(current);
            }

            return current;
        }

        private MergeDirectiveRuleDelegate CompileMergeDirectiveDelegate()
        {
            MergeDirectiveRuleDelegate current = (c, t) =>
            {
                if (t.Count > 0)
                {
                    throw new NotSupportedException(
                        "The type definitions could not be handled.");
                }
            };

            var handlers = new List<MergeDirectiveRuleFactory>();
            handlers.AddRange(_directiveMergeRules);
            handlers.Add(c => new DirectiveTypeMergeHandler(c).Merge);

            for (var i = handlers.Count - 1; i >= 0; i--)
            {
                current = handlers[i].Invoke(current);
            }

            return current;
        }

        public static SchemaMerger New() => new SchemaMerger();
    }
}
