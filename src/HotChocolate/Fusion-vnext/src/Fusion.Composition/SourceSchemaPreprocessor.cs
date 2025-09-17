using System.Collections.Immutable;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

/// <summary>
/// Preprocesses a source schema.
/// </summary>
internal sealed class SourceSchemaPreprocessor(
    MutableSchemaDefinition schema,
    SourceSchemaPreprocessorOptions? options = null)
{
    private readonly SourceSchemaPreprocessorOptions _options = options ?? new SourceSchemaPreprocessorOptions();

    public CompositionResult Process()
    {
        if (_options.ApplyInferredKeyDirectives)
        {
            ApplyInferredKeyDirectives();
        }

        return CompositionResult.Success();
    }

    /// <summary>
    /// Applies inferred key directives to types that are returned by lookup fields.
    /// </summary>
    private void ApplyInferredKeyDirectives()
    {
        var lookupFieldDefinitions =
            schema.Types
                .OfType<MutableComplexTypeDefinition>()
                .SelectMany(t => t.Fields.AsEnumerable().Where(f => f.HasLookupDirective()));

        foreach (var lookupFieldDefinition in lookupFieldDefinitions)
        {
            var fieldType = lookupFieldDefinition.Type.AsTypeDefinition();
            var possibleTypes = schema.GetPossibleTypes(fieldType);
            var lookupMap = lookupFieldDefinition.GetFusionLookupMap();
            var keyFields = lookupFieldDefinition.GetKeyFields(lookupMap, schema);

            foreach (var possibleType in possibleTypes)
            {
                possibleType.ApplyKeyDirective(keyFields);
            }

            if (fieldType is MutableInterfaceTypeDefinition interfaceType)
            {
                interfaceType.ApplyKeyDirective(keyFields);
            }
        }
    }
}
