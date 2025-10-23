using HotChocolate.Features;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Features;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.SyntaxWalkers;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaEnricher(MutableSchemaDefinition schema)
{
    public CompositionResult Enrich()
    {
        foreach (var type in schema.Types)
        {
            if (type is MutableComplexTypeDefinition complexType)
            {
                // Key fields.
                foreach (var keyField in GetKeyFields(complexType, schema))
                {
                    var sourceFieldMetadata = keyField.Features.GetOrSet<SourceFieldMetadata>();
                    sourceFieldMetadata.IsKeyField = true;
                }

                foreach (var field in complexType.Fields)
                {
                    EnrichField(field, complexType);
                }
            }
        }

        return CompositionResult.Success();
    }

    private static void EnrichField(MutableOutputFieldDefinition field, MutableComplexTypeDefinition complexType)
    {
        var sourceFieldMetadata = field.Features.GetOrSet<SourceFieldMetadata>();

        // Shareable fields.
        if (!field.HasExternalDirective()
            && (
                sourceFieldMetadata.IsKeyField
                || field.HasShareableDirective()
                || (complexType is MutableObjectTypeDefinition objectType && objectType.HasShareableDirective())))
        {
            sourceFieldMetadata.IsShareable = true;
        }
    }

    private static List<MutableOutputFieldDefinition> GetKeyFields(
        MutableComplexTypeDefinition complexType,
        MutableSchemaDefinition schema)
    {
        var keyDirectives = complexType.Directives.AsEnumerable().Where(d => d.Name == DirectiveNames.Key);
        var keyFields = new List<MutableOutputFieldDefinition>();

        foreach (var keyDirective in keyDirectives)
        {
            var fieldsArgument = ((StringValueNode)keyDirective.Arguments[ArgumentNames.Fields]).Value;
            var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{ {fieldsArgument} }}");
            var fieldsExtractor = new SelectionSetFieldsExtractor(schema);
            var fieldGroup = fieldsExtractor.ExtractFields(selectionSet, complexType);
            keyFields.AddRange(fieldGroup.Select(f => f.Field));
        }

        return keyFields;
    }
}
