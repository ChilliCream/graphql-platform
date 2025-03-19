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
                    queryField.ApplyLookupDirective();

                    resultObjectType.ApplyKeyDirective([keyField.Name]);
                }
            }
            else if (resultType is MutableInterfaceTypeDefinition resultInterfaceType)
            {
                if (resultInterfaceType.Fields.TryGetField(keyOutputFieldName, out var keyField))
                {
                    queryField.ApplyLookupDirective();

                    resultInterfaceType.ApplyKeyDirective([keyField.Name]);

                    foreach (var objectType in context.Schema.Types.OfType<MutableObjectTypeDefinition>())
                    {
                        if (objectType.Implements.ContainsName(resultInterfaceType.Name))
                        {
                            objectType.ApplyKeyDirective([keyField.Name]);
                        }
                    }
                }
            }
        }
    }

    private static void ApplyShareableToAllTypes(SourceSchemaPreprocessorContext context)
    {
        foreach (var objectType in context.Schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            objectType.ApplyShareableDirective();
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
