using System.Collections.Immutable;
using System.Text.RegularExpressions;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion;

/// <summary>
/// Preprocesses a source schema.
/// </summary>
internal sealed partial class SourceSchemaPreprocessor(
    MutableSchemaDefinition schema,
    ImmutableSortedSet<MutableSchemaDefinition> schemas,
    SourceSchemaPreprocessorOptions? options = null)
{
    private readonly SourceSchemaPreprocessorOptions _options = options ?? new SourceSchemaPreprocessorOptions();

    public CompositionResult Process()
    {
        var fusionV1CompatibilityMode = _options.Version.Major == 1;

        if (fusionV1CompatibilityMode)
        {
            RemoveDirectivesFromBatchFields();

            ApplyInferredLookupDirectives();
        }

        if (fusionV1CompatibilityMode || _options.ApplyInferredKeyDirectives)
        {
            ApplyInferredKeyDirectives();
        }

        if (fusionV1CompatibilityMode || _options.InheritInterfaceKeys)
        {
            InheritInterfaceKeys();
        }

        // We need to run this after keys have been inferred, so we do not attempt to mark them as @shareable.
        if (fusionV1CompatibilityMode)
        {
            ApplyShareableDirectives();
        }

        return CompositionResult.Success();
    }

    /// <summary>
    /// Applies @shareable to each field that is also present in another source schema.
    /// </summary>
    private void ApplyShareableDirectives()
    {
        foreach (var sourceSchema in schemas.Except([schema]))
        {
            foreach (var type in schema.Types.OfType<MutableObjectTypeDefinition>())
            {
                if (!sourceSchema.Types.TryGetType<MutableObjectTypeDefinition>(type.Name, out var otherType))
                {
                    continue;
                }

                var keyLookup = new HashSet<string>();
                foreach (var keyDirective in type.GetKeyDirectives())
                {
                    var fieldsArgument = (string)keyDirective.Arguments[ArgumentNames.Fields].Value!;
                    keyLookup.Add(fieldsArgument);
                }

                foreach (var field in type.Fields)
                {
                    if (keyLookup.Contains(field.Name))
                    {
                        continue;
                    }

                    if (field.Directives.ContainsName(Internal) || field.Directives.ContainsName(Inaccessible))
                    {
                        continue;
                    }

                    if (!otherType.Fields.TryGetField(field.Name, out var otherField)
                        || otherField.Directives.ContainsName(Internal)
                        || otherField.Directives.ContainsName(Inaccessible))
                    {
                        continue;
                    }

                    field.ApplyShareableDirective();
                }
            }
        }
    }

    /// <summary>
    /// Fusion v2 does not support batching fields, so we remove @lookup and @is from those,
    /// so they do not lead to a validation error later on.
    /// If the batching field is internal we remove it from the source schema.
    /// </summary>
    private void RemoveDirectivesFromBatchFields()
    {
        if (schema.QueryType is not { } queryType)
        {
            return;
        }

        var clonedQueryFields = queryType.Fields.AsEnumerable().ToArray();

        foreach (var field in clonedQueryFields)
        {
            var lookupDirective = field.Directives.FirstOrDefault(Lookup);

            if (!field.Type.IsListType() || lookupDirective is null)
            {
                continue;
            }

            if (field.Directives.ContainsName(Internal))
            {
                queryType.Fields.Remove(field);
            }
            else
            {
                field.Directives.Remove(lookupDirective);

                foreach (var argument in field.Arguments)
                {
                    var iSDirective = argument.Directives.FirstOrDefault(Is);

                    if (iSDirective is not null)
                    {
                        argument.Directives.Remove(iSDirective);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Applies an explicit @lookup to fields that would've been inferred as lookups
    /// in Fusion v1 by naming convention.
    /// </summary>
    private void ApplyInferredLookupDirectives()
    {
        if (schema.QueryType is not { } queryType)
        {
            return;
        }

        var lookupFieldConventionRegex = CreateLookupFieldConventionRegex();

        foreach (var field in queryType.Fields)
        {
            if (field.Directives.ContainsName(Lookup)
                || field.Type.IsListType()
                || field.Type.Kind == TypeKind.NonNull
                || field.Arguments.Count != 1)
            {
                continue;
            }

            var namedFieldType = field.Type.NamedType();
            var keyArgument = field.Arguments[0];

            if (field.Name is WellKnownFieldNames.Node
                && schema.Types.TryGetType<IInterfaceTypeDefinition>(WellKnownTypeNames.Node, out var nodeType)
                && namedFieldType == nodeType)
            {
                field.ApplyLookupDirective();
            }
            else if (namedFieldType is IObjectTypeDefinition objectType)
            {
                var parts = lookupFieldConventionRegex.Split(field.Name);

                if (parts.Length != 5)
                {
                    continue;
                }

                var typeName = parts[1];

                if (!typeName.Equals(namedFieldType.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var keyFieldNameByConvention = parts[3];
                var keyFieldByConvention = objectType.Fields.FirstOrDefault(f =>
                    f.Name.Equals(keyFieldNameByConvention, StringComparison.OrdinalIgnoreCase));

                if (keyFieldByConvention is not null)
                {
                    field.ApplyLookupDirective();

                    if (keyArgument.Name != keyFieldByConvention.Name)
                    {
                        var isDirective = keyArgument.Directives.FirstOrDefault(Is);

                        if (isDirective is not null)
                        {
                            if (isDirective.Arguments[ArgumentNames.Field] is StringValueNode fieldArgument
                                && fieldArgument.Value == keyFieldByConvention.Name)
                            {
                                continue;
                            }

                            keyArgument.Directives.Remove(isDirective);
                        }

                        keyArgument.ApplyIsDirective(keyFieldByConvention.Name);
                    }
                }
                else
                {
                    var @is = keyArgument.GetIsFieldSelectionMap();

                    var keyOutputFieldName = @is ?? keyArgument.Name;

                    if (objectType.Fields.ContainsName(keyOutputFieldName))
                    {
                        field.ApplyLookupDirective();
                    }
                }
            }
        }
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
            var possibleTypes = schema.GetPossibleTypes(fieldType).ToArray();

            try
            {
                foreach (var valueSelectionGroup in lookupFieldDefinition.GetValueSelectionGroups())
                {
                    var keyFields = lookupFieldDefinition.GetKeyFields(valueSelectionGroup, schema);

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

    [GeneratedRegex("^(.*?[a-z0-9])(By)([A-Z].*)$")]
    private static partial Regex CreateLookupFieldConventionRegex();
}
