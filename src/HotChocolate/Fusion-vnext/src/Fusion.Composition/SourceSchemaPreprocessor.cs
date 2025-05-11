using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Options;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

/// <summary>
/// Applies @lookup, @key, and optionally @shareable to a source schema to make it equivalent to a Fusion v1 source schema.
/// </summary>
internal sealed class SourceSchemaPreprocessor(
    MutableSchemaDefinition schema,
    SourceSchemaPreprocessorOptions? options = null)
{
    public MutableSchemaDefinition Process()
    {
        var context = new SourceSchemaPreprocessorContext(schema, options ?? new());

        ApplyLookups(context);

        if (context.Options.ApplyShareableToAllTypes)
        {
            ApplyShareableToAllTypes(context);
        }

        return schema;
    }

    private static void ApplyLookups(SourceSchemaPreprocessorContext context)
    {
        if (context.Schema.QueryType is not { } queryType)
        {
            return;
        }

        foreach (var queryField in queryType.Fields)
        {
            if (queryField.HasLookupDirective() ||
                queryField.Type.IsListType() ||
                queryField.Type.Kind == TypeKind.NonNull ||
                queryField.Arguments.Count != 1)
            {
                continue;
            }

            var keyArgument = queryField.Arguments.AsEnumerable().First();
            var @is = keyArgument.GetIsFieldSelectionMap();

            var queryFieldType = queryField.Type.AsTypeDefinition();
            var keyOutputFieldName = @is ?? keyArgument.Name;

            if (!context.Schema.Types.TryGetType(queryFieldType.Name, out var resultType))
            {
                continue;
            }

            if (resultType is MutableObjectTypeDefinition resultObjectType)
            {
                if (resultObjectType.Fields.TryGetField(keyOutputFieldName, out var keyField))
                {
                    ApplyLookupDirective(queryField, context);
                    ApplyKeyDirective(resultObjectType, [keyField.Name], context);
                }
            }
            else if (resultType is MutableInterfaceTypeDefinition resultInterfaceType)
            {
                if (resultInterfaceType.Fields.TryGetField(keyOutputFieldName, out var keyField))
                {
                    ApplyLookupDirective(queryField, context);
                    ApplyKeyDirective(resultInterfaceType, [keyField.Name], context);

                    foreach (var objectType in context.Schema.Types.OfType<MutableObjectTypeDefinition>())
                    {
                        if (objectType.Implements.ContainsName(resultInterfaceType.Name))
                        {
                            ApplyKeyDirective(objectType, [keyField.Name], context);
                        }
                    }
                }
            }
        }
    }

    private static void ApplyLookupDirective(
        MutableOutputFieldDefinition field,
        SourceSchemaPreprocessorContext context)
    {
        if (!context.Schema.DirectiveDefinitions.TryGetDirective(WellKnownDirectiveNames.Lookup,
            out var lookupDirectiveDefinition))
        {
            lookupDirectiveDefinition = new LookupMutableDirectiveDefinition();

            context.Schema.DirectiveDefinitions.Add(lookupDirectiveDefinition);
        }

        field.Directives.Add(new Directive(lookupDirectiveDefinition));
    }

    private static void ApplyKeyDirective(
        MutableComplexTypeDefinition declaringType,
        string[] fields,
        SourceSchemaPreprocessorContext context)
    {
        if (!context.Schema.DirectiveDefinitions.TryGetDirective(WellKnownDirectiveNames.Key,
            out var keyDirectiveDefinition))
        {
            if (!context.Schema.Types.TryGetType(WellKnownTypeNames.FieldSelectionSet, out var untypedType) ||
                untypedType is not MutableScalarTypeDefinition fieldSelectionSetType)
            {
                fieldSelectionSetType = MutableScalarTypeDefinition.Create(WellKnownTypeNames.FieldSelectionSet);

                context.Schema.Types.Add(fieldSelectionSetType);
            }

            keyDirectiveDefinition = new KeyMutableDirectiveDefinition(fieldSelectionSetType);

            context.Schema.DirectiveDefinitions.Add(keyDirectiveDefinition);
        }

        var fieldsArgument = new ArgumentAssignment(WellKnownArgumentNames.Fields, string.Join(" ", fields));
        var keyDirective = new Directive(keyDirectiveDefinition, fieldsArgument);

        declaringType.Directives.Add(keyDirective);
    }

    private static void ApplyShareableToAllTypes(SourceSchemaPreprocessorContext context)
    {
        context.Schema.DirectiveDefinitions
            .TryGetDirective(WellKnownDirectiveNames.Shareable, out var shareableDirectiveDefinition);

        foreach (var objectType in context.Schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            if (shareableDirectiveDefinition is null)
            {
                shareableDirectiveDefinition = new ShareableMutableDirectiveDefinition();

                context.Schema.DirectiveDefinitions.Add(shareableDirectiveDefinition);
            }

            objectType.Directives.Add(new Directive(shareableDirectiveDefinition));
        }
    }
}

internal sealed class SourceSchemaPreprocessorContext(
    MutableSchemaDefinition schema,
    SourceSchemaPreprocessorOptions options)
{
    public MutableSchemaDefinition Schema => schema;

    public SourceSchemaPreprocessorOptions Options => options;
}
