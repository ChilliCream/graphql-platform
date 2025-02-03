using HotChocolate.Fusion.Extensions;
using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion;

/// <summary>
/// Applies @lookup, @key, and optionally @shareable to a source schema to make it equivalent to a Fusion v1 source schema.
/// </summary>
internal class SourceSchemaPreProcessor(SchemaDefinition schemaDefinition, SourceSchemaPreProcessorOptions? options = null)
{
    public SchemaDefinition Process()
    {
        var context = new SourceSchemaPreProcessorContext(schemaDefinition, options ?? new());

        ApplyLookups(context);

        if (context.Options.ApplyShareableToAllTypes)
        {
            ApplyShareableToAllTypes(context);
        }

        return schemaDefinition;
    }

    private static void ApplyLookups(SourceSchemaPreProcessorContext context)
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

            var keyArgument = queryField.Arguments.First();
            var @is = keyArgument.GetIsFieldSelectionMap();

            var queryFieldType = queryField.Type.NamedType();
            var keyOutputFieldName = @is ?? keyArgument.Name;

            if (!context.Schema.Types.TryGetType(queryFieldType.Name, out var resultType))
            {
                continue;
            }

            if (resultType is ObjectTypeDefinition resultObjectType)
            {
                if (resultObjectType.Fields.TryGetField(keyOutputFieldName, out var keyField))
                {
                    queryField.ApplyLookupDirective();

                    resultObjectType.ApplyKeyDirective([keyField.Name]);
                }
            }
            else if (resultType is InterfaceTypeDefinition resultInterfaceType)
            {
                if (resultInterfaceType.Fields.TryGetField(keyOutputFieldName, out var keyField))
                {
                    queryField.ApplyLookupDirective();

                    resultInterfaceType.ApplyKeyDirective([keyField.Name]);

                    foreach (var objectType in context.Schema.Types.OfType<ObjectTypeDefinition>())
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

    private void ApplyShareableToAllTypes(SourceSchemaPreProcessorContext context)
    {
        foreach (var objectType in context.Schema.Types.OfType<ObjectTypeDefinition>())
        {
            objectType.ApplyShareableDirective();
        }
    }
}

internal sealed class SourceSchemaPreProcessorContext(SchemaDefinition schemaDefinition, SourceSchemaPreProcessorOptions options)
{
    public SchemaDefinition Schema => schemaDefinition;

    public SourceSchemaPreProcessorOptions Options => options;
}

internal sealed class SourceSchemaPreProcessorOptions
{
    public bool ApplyShareableToAllTypes { get; set; } = true;
}
