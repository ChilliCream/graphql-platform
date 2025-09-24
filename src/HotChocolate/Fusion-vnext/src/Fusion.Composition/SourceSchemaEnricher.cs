using System.Collections.Immutable;
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

internal sealed class SourceSchemaEnricher(
    MutableSchemaDefinition schema,
    ImmutableSortedSet<MutableSchemaDefinition> schemas)
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

    private void EnrichField(
        MutableOutputFieldDefinition field,
        MutableComplexTypeDefinition complexType)
    {
        var sourceFieldMetadata = field.Features.GetOrSet<SourceFieldMetadata>();

        sourceFieldMetadata.IsExternal = field.HasExternalDirective();
        sourceFieldMetadata.IsInternal =
            field.HasInternalDirective()
            || (complexType is MutableObjectTypeDefinition objectType && objectType.HasInternalDirective());

        sourceFieldMetadata.HasShareableDirective = field.HasShareableDirective();

        // Overridden fields.
        foreach (var sourceSchema in schemas.Except([schema]))
        {
            if (sourceSchema.Types.TryGetType(complexType.Name, out var sourceType)
                && sourceType is MutableComplexTypeDefinition sourceComplexType
                && sourceComplexType.Fields.TryGetField(field.Name, out var sourceField)
                && sourceField.GetOverrideFrom() == schema.Name)
            {
                sourceFieldMetadata.IsOverridden = true;
            }
        }

        // Shareable fields.
        if (!field.HasExternalDirective()
            && (
                sourceFieldMetadata.IsKeyField
                || field.HasShareableDirective()
                || (complexType is MutableObjectTypeDefinition o && o.HasShareableDirective())))
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
            if (!keyDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var fieldsValueNode)
                || fieldsValueNode is not StringValueNode stringValueNode)
            {
                continue;
            }

            try
            {
                var fieldsArgument = stringValueNode.Value;
                var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{ {fieldsArgument} }}");
                var fieldsExtractor = new SelectionSetFieldsExtractor(schema);
                var fieldGroup = fieldsExtractor.ExtractFields(selectionSet, complexType);
                keyFields.AddRange(fieldGroup.Select(f => f.Field));
            }
            catch (SyntaxException)
            {
                // Validated later.
            }
        }

        return keyFields;
    }
}
