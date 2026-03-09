using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Features;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.SyntaxWalkers;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaEnricher(
    MutableSchemaDefinition schema,
    ImmutableSortedSet<MutableSchemaDefinition> schemas)
{
    public CompositionResult Enrich()
    {
        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case MutableComplexTypeDefinition complexType:
                    AddKeyFieldsInfo(complexType, schema);

                    if (type is MutableObjectTypeDefinition objectType)
                    {
                        EnrichObjectType(objectType);
                    }

                    foreach (var field in complexType.Fields)
                    {
                        EnrichOutputField(field);

                        foreach (var argument in field.Arguments)
                        {
                            EnrichInputField(argument);
                        }
                    }

                    break;

                case MutableEnumTypeDefinition enumType:
                    EnrichEnumType(enumType);
                    break;
            }
        }

        return CompositionResult.Success();
    }

    private static void EnrichObjectType(MutableObjectTypeDefinition objectType)
    {
        var sourceMetadata = objectType.Features.GetOrSet<SourceObjectTypeMetadata>();
        sourceMetadata.HasShareableDirective = objectType.Directives.ContainsName(Shareable);
        sourceMetadata.IsInternal = objectType.Directives.ContainsName(WellKnownDirectiveNames.Internal);
    }

    private static void EnrichEnumType(MutableEnumTypeDefinition enumType)
    {
        foreach (var enumValue in enumType.Values)
        {
            var sourceMetadata = enumValue.Features.GetOrSet<SourceEnumValueMetadata>();
            sourceMetadata.IsInaccessible =
                enumValue.Directives.ContainsName(Inaccessible)
                || enumType.Directives.ContainsName(Inaccessible);
        }
    }

    private void EnrichOutputField(MutableOutputFieldDefinition outputField)
    {
        var declaringType = outputField.DeclaringMember!;
        var sourceMetadata = outputField.Features.GetOrSet<SourceOutputFieldMetadata>();

        sourceMetadata.IsInaccessible =
            outputField.Directives.ContainsName(Inaccessible)
            || declaringType.Directives.ContainsName(Inaccessible);
        sourceMetadata.IsInternal =
            outputField.Directives.ContainsName(WellKnownDirectiveNames.Internal)
            || declaringType.Directives.ContainsName(WellKnownDirectiveNames.Internal);
        sourceMetadata.IsLookup = outputField.Directives.ContainsName(Lookup);
        sourceMetadata.HasExternalDirective = outputField.Directives.ContainsName(External);
        sourceMetadata.HasInternalDirective = outputField.Directives.ContainsName(WellKnownDirectiveNames.Internal);
        sourceMetadata.HasOverrideDirective = outputField.Directives.ContainsName(Override);
        sourceMetadata.HasProvidesDirective = outputField.Directives.ContainsName(Provides);
        sourceMetadata.HasShareableDirective = outputField.Directives.ContainsName(Shareable);

        // Overridden fields.
        foreach (var sourceSchema in schemas.Except([schema]))
        {
            if (sourceSchema.Types.TryGetType(declaringType.Name, out var sourceType)
                && sourceType is MutableComplexTypeDefinition sourceComplexType
                && sourceComplexType.Fields.TryGetField(outputField.Name, out var sourceField)
                && sourceField.GetOverrideFrom() == schema.Name)
            {
                sourceMetadata.IsOverridden = true;
            }
        }

        // Provides fields.
        AddProvidesFieldsInfo(outputField, schema);

        // Shareable fields.
        if (!outputField.IsExternal
            && (
                sourceMetadata.IsKeyField
                || sourceMetadata.HasShareableDirective
                || declaringType is MutableObjectTypeDefinition { HasShareableDirective: true }))
        {
            sourceMetadata.IsShareable = true;
        }
    }

    private static void EnrichInputField(MutableInputFieldDefinition inputField)
    {
        var sourceMetadata = inputField.Features.GetOrSet<SourceInputFieldMetadata>();

        sourceMetadata.HasIsDirective = inputField.Directives.ContainsName(Is);
        sourceMetadata.HasRequireDirective = inputField.Directives.ContainsName(Require);
        sourceMetadata.IsInaccessible =
            inputField.Directives.ContainsName(Inaccessible)
            // Field argument (field or field's declaring type is inaccessible).
            || inputField.DeclaringMember is MutableOutputFieldDefinition { IsInaccessible: true }
            // Input field (input type is inaccessible).
            || (
                inputField.DeclaringMember is MutableInputObjectTypeDefinition inputObjectType
                && inputObjectType.Directives.ContainsName(Inaccessible));

        AddIsFieldInfo(inputField);
        AddRequireFieldInfo(inputField);
    }

    private static void AddProvidesFieldsInfo(
        MutableOutputFieldDefinition outputField,
        MutableSchemaDefinition schema)
    {
        var providesDirective =
            outputField.Directives.AsEnumerable().SingleOrDefault(d => d.Name == Provides);

        if (providesDirective is null)
        {
            return;
        }

        var providesInfo = new ProvidesInfo(providesDirective);

        if (!providesDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var f)
            || f is not StringValueNode fieldsArgument)
        {
            providesInfo.IsInvalidFieldsType = true;
        }
        else
        {
            try
            {
                var selectionSet = ParseSelectionSet($"{{ {fieldsArgument.Value} }}");

                providesInfo.SelectionSet = selectionSet;

                providesInfo.FieldNodes.AddRange(
                    new SelectionSetFieldNodesExtractor().ExtractFieldNodes(selectionSet));

                var fieldsExtractor = new SelectionSetFieldsExtractor(schema);
                var fieldGroup = fieldsExtractor.ExtractFields(selectionSet, outputField.Type.AsTypeDefinition());

                providesInfo.Fields.AddRange(fieldGroup.Select(i => i.Field));
            }
            catch (SyntaxException)
            {
                providesInfo.IsInvalidFieldsSyntax = true;
            }
        }

        var sourceMetadata = outputField.Features.GetOrSet<SourceOutputFieldMetadata>();
        sourceMetadata.ProvidesInfo = providesInfo;
    }

    private static void AddKeyFieldsInfo(
        MutableComplexTypeDefinition complexType,
        MutableSchemaDefinition schema)
    {
        var keyDirectives = complexType.Directives.AsEnumerable().Where(d => d.Name == Key);

        foreach (var keyDirective in keyDirectives)
        {
            var keyInfo = new KeyInfo();

            if (!keyDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var fieldsValueNode)
                || fieldsValueNode is not StringValueNode stringValueNode)
            {
                keyInfo.IsInvalidFieldsType = true;
            }
            else
            {
                try
                {
                    var fieldsArgument = stringValueNode.Value;
                    var selectionSet = ParseSelectionSet($"{{ {fieldsArgument} }}");

                    keyInfo.FieldNodes.AddRange(
                        new SelectionSetFieldNodesExtractor().ExtractFieldNodes(selectionSet));

                    var fieldsExtractor = new SelectionSetFieldsExtractor(schema);
                    var fieldGroup = fieldsExtractor.ExtractFields(selectionSet, complexType);
                    var keyFields = fieldGroup.Select(i => i.Field).ToList();

                    keyInfo.Fields.AddRange(keyFields);

                    foreach (var keyField in keyFields)
                    {
                        var sourceFieldMetadata = keyField.Features.GetOrSet<SourceOutputFieldMetadata>();
                        sourceFieldMetadata.IsKeyField = true;
                    }
                }
                catch (SyntaxException)
                {
                    keyInfo.IsInvalidFieldsSyntax = true;
                }
            }

            var sourceMetadata = complexType.Features.GetOrSet<SourceComplexTypeMetadata>();
            sourceMetadata.KeyInfoByDirective[keyDirective] = keyInfo;
        }
    }

    private static void AddIsFieldInfo(MutableInputFieldDefinition inputField)
    {
        var isDirective = inputField.Directives.AsEnumerable().SingleOrDefault(d => d.Name == Is);

        if (isDirective is null)
        {
            return;
        }

        var isInfo = new IsInfo(isDirective);

        if (!isDirective.Arguments.TryGetValue(ArgumentNames.Field, out var f)
            || f is not StringValueNode fieldArgument)
        {
            isInfo.IsInvalidFieldType = true;
        }
        else
        {
            try
            {
                new FieldSelectionMapParser(fieldArgument.Value).Parse();
            }
            catch (FieldSelectionMapSyntaxException)
            {
                isInfo.IsInvalidFieldSyntax = true;
            }
        }

        var sourceMetadata = inputField.Features.GetOrSet<SourceInputFieldMetadata>();
        sourceMetadata.IsInfo = isInfo;
    }

    private static void AddRequireFieldInfo(MutableInputFieldDefinition inputField)
    {
        var requireDirective = inputField.Directives.AsEnumerable().SingleOrDefault(d => d.Name == Require);

        if (requireDirective is null)
        {
            return;
        }

        var requireInfo = new RequireInfo(requireDirective);

        if (!requireDirective.Arguments.TryGetValue(ArgumentNames.Field, out var f)
            || f is not StringValueNode fieldArgument)
        {
            requireInfo.IsInvalidFieldType = true;
        }
        else
        {
            try
            {
                new FieldSelectionMapParser(fieldArgument.Value).Parse();
            }
            catch (FieldSelectionMapSyntaxException)
            {
                requireInfo.IsInvalidFieldSyntax = true;
            }
        }

        var sourceMetadata = inputField.Features.GetOrSet<SourceInputFieldMetadata>();
        sourceMetadata.RequireInfo = requireInfo;
    }
}
