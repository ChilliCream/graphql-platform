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
            var shareableDirectiveDefinition = new ShareableMutableDirectiveDefinition();

            // TODO: Check that these do not already exist
            context.Schema.DirectiveDefinitions.Add(shareableDirectiveDefinition);

            ApplyShareableToAllTypes(context, shareableDirectiveDefinition);
        }

        return schema;
    }

    private static void ApplyLookups(SourceSchemaPreprocessorContext context)
    {
        if (context.Schema.QueryType is not { } queryType)
        {
            return;
        }

        var fieldSelectionSetType = MutableScalarTypeDefinition.Create(WellKnownTypeNames.FieldSelectionSet);
        var keyDirectiveDefinition = new KeyMutableDirectiveDefinition(fieldSelectionSetType);
        var lookupDirectiveDefinition = new LookupMutableDirectiveDefinition();

        // TODO: Check that these do not already exist
        context.Schema.DirectiveDefinitions.Add(keyDirectiveDefinition);
        context.Schema.DirectiveDefinitions.Add(lookupDirectiveDefinition);

        foreach (var queryField in queryType.Fields)
        {
            if (queryField.HasLookupDirective()
                || queryField.Type.IsListType()
                || queryField.Type.Kind == TypeKind.NonNull
                || queryField.Arguments.Count != 1)
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
                    queryField.ApplyLookupDirective(lookupDirectiveDefinition);

                    resultObjectType.ApplyKeyDirective(keyDirectiveDefinition, [keyField.Name]);
                }
            }
            else if (resultType is MutableInterfaceTypeDefinition resultInterfaceType)
            {
                if (resultInterfaceType.Fields.TryGetField(keyOutputFieldName, out var keyField))
                {
                    queryField.ApplyLookupDirective(lookupDirectiveDefinition);

                    resultInterfaceType.ApplyKeyDirective(keyDirectiveDefinition, [keyField.Name]);

                    foreach (var objectType in context.Schema.Types.OfType<MutableObjectTypeDefinition>())
                    {
                        if (objectType.Implements.ContainsName(resultInterfaceType.Name))
                        {
                            objectType.ApplyKeyDirective(keyDirectiveDefinition, [keyField.Name]);
                        }
                    }
                }
            }
        }
    }

    private static void ApplyShareableToAllTypes(SourceSchemaPreprocessorContext context,
        ShareableMutableDirectiveDefinition shareableDirectiveDefinition)
    {
        foreach (var objectType in context.Schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            objectType.ApplyShareableDirective(shareableDirectiveDefinition);
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
