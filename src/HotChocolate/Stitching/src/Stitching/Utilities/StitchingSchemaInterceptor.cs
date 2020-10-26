using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Introspection;
using IHasName = HotChocolate.Types.IHasName;
using static HotChocolate.Stitching.DirectiveFieldNames;

namespace HotChocolate.Stitching.Utilities
{
    internal class StitchingSchemaInterceptor : SchemaInterceptor
    {
        public override void OnBeforeCreate(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder)
        {
            var allSchemas = new OrderedDictionary<NameString, DocumentNode>();

            foreach (KeyValuePair<NameString, IRequestExecutor> executor in
                context.GetRemoteExecutors())
            {
                allSchemas.Add(executor.Key, executor.Value.Schema.ToDocument());
            }

            IReadOnlyList<DocumentNode> typeExtensions = context.GetTypeExtensions();

            // merge schemas
            DocumentNode mergedSchema = MergeSchemas(context, allSchemas);
            mergedSchema = AddExtensions(mergedSchema, typeExtensions);
            mergedSchema = RewriteMerged(context, mergedSchema);
            mergedSchema = mergedSchema.RemoveBuiltInTypes();

            VisitMerged(context, mergedSchema);
            MarkExternalFields(schemaBuilder, mergedSchema);
            BuildNameLookup(context, schemaBuilder, mergedSchema, allSchemas.Keys);

            schemaBuilder
                .AddDocument(mergedSchema)
                .AddDirectiveType<DelegateDirectiveType>()
                .AddDirectiveType<ComputedDirectiveType>()
                .AddDirectiveType<SourceDirectiveType>()
                .SetTypeResolver(IsOfTypeFallback);
        }

        private static DocumentNode MergeSchemas(
            IDescriptorContext context,
            IDictionary<NameString, DocumentNode> schemas)
        {
            var merger = new SchemaMerger();

            foreach (NameString name in schemas.Keys)
            {
                merger.AddSchema(name, schemas[name]);
            }

            foreach (MergeTypeRuleFactory handler in context.GetTypeMergeRules())
            {
                merger.AddTypeMergeRule(handler);
            }

            foreach (MergeDirectiveRuleFactory handler in context.GetDirectiveMergeRules())
            {
                merger.AddDirectiveMergeRule(handler);
            }

            foreach (IDocumentRewriter rewriter in context.GetDocumentRewriter())
            {
                merger.AddDocumentRewriter(rewriter);
            }

            foreach (ITypeRewriter rewriter in context.GetTypeRewriter())
            {
                merger.AddTypeRewriter(rewriter);
            }

            return merger.Merge();
        }

        private static DocumentNode AddExtensions(
            DocumentNode schema,
            IReadOnlyCollection<DocumentNode> typeExtensions)
        {
            if (typeExtensions.Count == 0)
            {
                return schema;
            }

            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode currentSchema = schema;

            foreach (DocumentNode extension in typeExtensions)
            {
                currentSchema = rewriter.AddExtensions(
                    currentSchema, extension);
            }

            return currentSchema;
        }

        private static DocumentNode RewriteMerged(IDescriptorContext context, DocumentNode schema)
        {
            IReadOnlyList<Func<DocumentNode, DocumentNode>> mergedDocRewriter =
                context.GetMergedDocRewriter();

            if (mergedDocRewriter.Count == 0)
            {
                return schema;
            }

            DocumentNode current = schema;

            foreach (Func<DocumentNode, DocumentNode> rewriter in mergedDocRewriter)
            {
                current = rewriter.Invoke(current);
            }

            return current;
        }

        private static void VisitMerged(IDescriptorContext context, DocumentNode schema)
        {
            foreach (Action<DocumentNode> visitor in context.GetMergedDocVisitors())
            {
                visitor.Invoke(schema);
            }
        }

        private static void MarkExternalFields(ISchemaBuilder schemaBuilder, DocumentNode document)
        {
            Dictionary<NameString, ISet<NameString>> externalFieldLookup =
                new Dictionary<NameString, ISet<NameString>>();

            foreach (ObjectTypeDefinitionNodeBase objectType in
                document.Definitions.OfType<ObjectTypeDefinitionNodeBase>())
            {
                if (!externalFieldLookup.TryGetValue(
                    objectType.Name.Value,
                    out ISet<NameString>? externalFields))
                {
                    externalFields = new HashSet<NameString>();
                    externalFieldLookup.Add(objectType.Name.Value, externalFields);
                }

                MarkExternalFields(objectType.Fields, externalFields);
            }

            schemaBuilder.AddExternalFieldLookup(externalFieldLookup);
        }

        private static void BuildNameLookup(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder,
            DocumentNode document,
            ICollection<NameString> schemaNames)
        {
            Dictionary<(NameString Type, NameString TargetSchema), NameString> nameLookup =
                new Dictionary<(NameString, NameString), NameString>();

            foreach (INamedSyntaxNode type in document.Definitions.OfType<INamedSyntaxNode>())
            {
                foreach (DirectiveNode directive in type.Directives
                    .Where(t => t.Name.Value.EqualsOrdinal(DirectiveNames.Source)))
                {
                    if (directive.Arguments.FirstOrDefault(
                        t => t.Name.Value.EqualsOrdinal(Source_Schema))?.Value
                            is StringValueNode schema &&
                        directive.Arguments.FirstOrDefault(
                            t => t.Name.Value.EqualsOrdinal(Source_Name))?.Value
                                is StringValueNode name &&
                        !name.Value.EqualsOrdinal(type.Name.Value))
                    {
                        nameLookup[(type.Name.Value, schema.Value)] = name.Value;
                    }
                }
            }

            foreach (RenameTypeRewriter rewriter in
                context.GetTypeRewriter().OfType<RenameTypeRewriter>())
            {
                if (rewriter.SchemaName is null)
                {
                    foreach (NameString schemaName in schemaNames)
                    {
                        nameLookup[(rewriter.NewTypeName, schemaName)] =
                            rewriter.OriginalTypeName;
                    }
                }
                else
                {
                    nameLookup[(rewriter.NewTypeName, rewriter.SchemaName.Value)] =
                        rewriter.OriginalTypeName;
                }
            }

            schemaBuilder.AddNameLookup(nameLookup);
        }

        private static void MarkExternalFields(
            IReadOnlyList<FieldDefinitionNode> fields,
            ISet<NameString> externalFields)
        {
            foreach (FieldDefinitionNode field in fields)
            {
                if (field.Directives.Count == 0 ||
                    field.Directives.All(t => !t.Name.Value.EqualsOrdinal(DirectiveNames.Computed)))
                {
                    externalFields.Add(field.Name.Value);
                }
            }
        }

        private static bool IsOfTypeFallback(
            ObjectType objectType,
            IResolverContext context,
            object resolverResult)
        {
            if (resolverResult is IReadOnlyDictionary<string, object> dict)
            {
                if (dict.TryGetValue(WellKnownFieldNames.TypeName, out object? value) &&
                    TryDeserializeTypeName(value, out string? typeName))
                {
                    if (objectType.Directives.Contains(DirectiveNames.Source) &&
                        context.ScopedContextData.TryGetValue(WellKnownContextData.SchemaName, out object? o) &&
                        o is NameString schemaName &&
                        objectType.TryGetSourceDirective(schemaName, out SourceDirective? source))
                    {
                        return source.Name.Equals(typeName);
                    }
                    return objectType.Name.Equals(typeName);
                }
            }
            else if (objectType.RuntimeType == typeof(object))
            {
                return IsOfTypeWithName(objectType, resolverResult);
            }

            return IsOfTypeWithClrType(objectType, resolverResult);
        }

        private static bool TryDeserializeTypeName(
            object serializedTypeName,
            [NotNullWhen(true)] out string? typeName)
        {
            if (serializedTypeName is string s)
            {
                typeName = s;
                return true;
            }

            if (serializedTypeName is StringValueNode sv)
            {
                typeName = sv.Value;
                return true;
            }

            typeName = null;
            return false;
        }

        private static bool IsOfTypeWithClrType(IHasRuntimeType type, object? result) =>
            result is null || type.RuntimeType.IsInstanceOfType(result);

        private static bool IsOfTypeWithName(IHasName objectType, object? result) =>
            result == null || objectType.Name.Equals(result.GetType().Name);
    }
}
