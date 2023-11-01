using System.Diagnostics.CodeAnalysis;
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
using static HotChocolate.Language.SyntaxKind;
using IHasName = HotChocolate.Types.IHasName;
using static HotChocolate.Stitching.DirectiveFieldNames;

namespace HotChocolate.Stitching.Utilities;

internal sealed class StitchingSchemaInterceptor : TypeInterceptor
{
    internal override void OnBeforeCreateSchemaInternal(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder)
    {
        var allSchemas = new OrderedDictionary<string, DocumentNode>();

        foreach (var executor in
            context.GetRemoteExecutors())
        {
            allSchemas.Add(executor.Key, executor.Value.Schema.ToDocument(true));
        }

        var typeExtensions = context.GetTypeExtensions();

        // merge schemas
        var mergedSchema = MergeSchemas(context, allSchemas);
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
        IDictionary<string, DocumentNode> schemas)
    {
        var merger = new SchemaMerger();

        foreach (var name in schemas.Keys)
        {
            merger.AddSchema(name, schemas[name]);
        }

        foreach (var handler in context.GetTypeMergeRules())
        {
            merger.AddTypeMergeRule(handler);
        }

        foreach (var handler in context.GetDirectiveMergeRules())
        {
            merger.AddDirectiveMergeRule(handler);
        }

        foreach (var rewriter in context.GetDocumentRewriter())
        {
            merger.AddDocumentRewriter(rewriter);
        }

        foreach (var rewriter in context.GetTypeRewriter())
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
        var currentSchema = schema;

        foreach (var extension in typeExtensions)
        {
            currentSchema = rewriter.AddExtensions(
                currentSchema,
                extension);
        }

        return currentSchema;
    }

    private static DocumentNode RewriteMerged(IDescriptorContext context, DocumentNode schema)
    {
        var mergedDocRewriter =
            context.GetMergedDocRewriter();

        if (mergedDocRewriter.Count == 0)
        {
            return schema;
        }

        var current = schema;

        foreach (var rewriter in mergedDocRewriter)
        {
            current = rewriter.Invoke(current);
        }

        return current;
    }

    private static void VisitMerged(IDescriptorContext context, DocumentNode schema)
    {
        foreach (var visitor in context.GetMergedDocVisitors())
        {
            visitor.Invoke(schema);
        }
    }

    private static void MarkExternalFields(ISchemaBuilder schemaBuilder, DocumentNode document)
    {
        var externalFieldLookup =
            new Dictionary<string, ISet<string>>();

        foreach (var objectType in document.Definitions)
        {
            if (objectType.Kind is ObjectTypeDefinition or SyntaxKind.ObjectTypeExtension)
            {
                if (!externalFieldLookup.TryGetValue(
                    ((ComplexTypeDefinitionNodeBase)objectType).Name.Value,
                    out var externalFields))
                {
                    externalFields = new HashSet<string>();
                    externalFieldLookup.Add(
                        ((ComplexTypeDefinitionNodeBase)objectType).Name.Value,
                        externalFields);
                }

                MarkExternalFields(
                    ((ComplexTypeDefinitionNodeBase)objectType).Fields,
                    externalFields);
            }
        }

        schemaBuilder.AddExternalFieldLookup(externalFieldLookup);
    }

    private static void BuildNameLookup(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder,
        DocumentNode document,
        ICollection<string> schemaNames)
    {
        Dictionary<(string Type, string TargetSchema), string> nameLookup = new();

        foreach (var type in document.Definitions.OfType<INamedSyntaxNode>())
        {
            foreach (var directive in type.Directives
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

        foreach (var rewriter in
            context.GetTypeRewriter().OfType<RenameTypeRewriter>())
        {
            if (rewriter.SchemaName is null)
            {
                foreach (var schemaName in schemaNames)
                {
                    nameLookup[(rewriter.NewTypeName, schemaName)] =
                        rewriter.OriginalTypeName;
                }
            }
            else
            {
                nameLookup[(rewriter.NewTypeName, rewriter.SchemaName)] =
                    rewriter.OriginalTypeName;
            }
        }

        schemaBuilder.AddNameLookup(nameLookup);
    }

    private static void MarkExternalFields(
        IReadOnlyList<FieldDefinitionNode> fields,
        ISet<string> externalFields)
    {
        foreach (var field in fields)
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
            if (dict.TryGetValue(WellKnownFieldNames.TypeName, out var value) &&
                TryDeserializeTypeName(value, out var typeName))
            {
                if (objectType.Directives.ContainsDirective(DirectiveNames.Source) &&
                    context.ScopedContextData.TryGetValue(
                        WellKnownContextData.SchemaName,
                        out var o) &&
                    o is string schemaName &&
                    objectType.TryGetSourceDirective(schemaName, out var source))
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
