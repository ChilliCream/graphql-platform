using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

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

        if (_options.InheritInterfaceKeys)
        {
            InheritInterfaceKeys();
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
                .SelectMany(t => t.Fields.AsEnumerable().Where(f => f.Directives.ContainsName(Lookup)));

        foreach (var lookupFieldDefinition in lookupFieldDefinitions)
        {
            var fieldType = lookupFieldDefinition.Type.AsTypeDefinition();
            var possibleTypes = schema.GetPossibleTypes(fieldType);
            var lookupMap = lookupFieldDefinition.GetFusionLookupMap();

            try
            {
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
            catch (FieldSelectionMapSyntaxException)
            {
                // Validated later.
            }
        }
    }

    /// <summary>
    /// Applies key directives to types based on the keys defined on the interfaces that they
    /// implement.
    /// </summary>
    private void InheritInterfaceKeys()
    {
        foreach (var complexType in schema.Types.OfType<MutableComplexTypeDefinition>())
        {
            foreach (var interfaceType in complexType.Implements)
            {
                foreach (var keyDirective in interfaceType.GetKeyDirectives())
                {
                    var fieldsArgument = keyDirective.Arguments[ArgumentNames.Fields].Value!;
                    complexType.ApplyKeyDirective((string)fieldsArgument);
                }
            }
        }
    }
}
