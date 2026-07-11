using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Validators;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using StringValueNode = HotChocolate.Language.StringValueNode;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Removes fields marked with <c>@external</c> from complex types. External fields
/// come in three flavors that are treated differently:
/// <list type="bullet">
/// <item>
/// <description>
/// Referenced by a <c>@key</c> selection: retained with <c>@external</c> removed so
/// they become full contributions (a key implies the source schema can resolve them).
/// </description>
/// </item>
/// <item>
/// <description>
/// Referenced by a <c>@provides</c> selection: retained with <c>@external</c> intact so
/// the merger marks them <c>@fusion__field(partial: true)</c> (path-scoped, non-resolvable).
/// </description>
/// </item>
/// <item>
/// <description>
/// Referenced by a <c>@require</c> field-selection map: retained with <c>@external</c>
/// intact for the same reason. A <c>@require</c> reference is an input, never a resolvable
/// contribution, so its <c>@external</c> marker must survive.
/// </description>
/// </item>
/// </list>
/// External fields referenced by none of the above are removed.
/// </summary>
internal static class RemoveExternalFields
{
    /// <summary>
    /// Removes unreferenced <c>@external</c> fields from the schema.
    /// External fields that are the target of a <c>@provides</c> selection or a
    /// <c>@require</c> field-selection map on the same subgraph are kept with
    /// <c>@external</c> intact so the downstream Composite Schema Spec validator and
    /// planner can see them as non-resolvable contributions. External fields that are
    /// part of a <c>@key</c> selection are retained with <c>@external</c> removed so
    /// they become full contributions.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        var providesReferences = CollectProvidesReferences(schema);
        var keyReferences = CollectKeyReferences(schema);
        var requireReferences = CollectRequireReferences(schema);
        var emptyObjectTypes = new List<MutableObjectTypeDefinition>();

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            var externalFields = new List<MutableOutputFieldDefinition>();

            foreach (var field in complexType.Fields)
            {
                if (!field.Directives.ContainsName(FederationDirectiveNames.External))
                {
                    continue;
                }

                if (keyReferences.Contains((complexType.Name, field.Name))
                    || SourceExternalFieldMetadata.Contains(
                        schema,
                        complexType.Name,
                        field.Name))
                {
                    var externalDirective = field.Directives.FirstOrDefault(
                        FederationDirectiveNames.External);

                    if (externalDirective is not null)
                    {
                        field.Directives.Remove(externalDirective);
                    }
                }
                else if (!providesReferences.Contains((complexType.Name, field.Name))
                    && !requireReferences.Contains((complexType.Name, field.Name)))
                {
                    externalFields.Add(field);
                }
            }

            foreach (var field in externalFields)
            {
                complexType.Fields.Remove(field);
            }

            if (complexType is MutableObjectTypeDefinition objectType
                && objectType.Fields.Count == 0)
            {
                emptyObjectTypes.Add(objectType);
            }
        }

        foreach (var objectType in emptyObjectTypes)
        {
            if (!IsReferencedByOutputField(schema, objectType))
            {
                schema.Types.Remove(objectType.Name);
            }
        }
    }

    /// <summary>
    /// Removes <c>@external</c> fields that are no longer referenced by any <c>@provides</c>
    /// selection or <c>@require</c> field-selection map in the schema. This is invoked by the
    /// fixed-point prune after a reachability pass removes a requirement-carrying type, which
    /// orphans the externals that type's selections referenced. It reuses the same collectors
    /// that <see cref="Apply"/> uses to preserve externals during preprocessing, so an external
    /// is dropped exactly when it would otherwise be reported as <c>EXTERNAL_UNUSED</c>.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    /// <returns>
    /// <c>true</c> if at least one field was removed; otherwise, <c>false</c>.
    /// </returns>
    internal static bool RemoveDeadExternalFields(MutableSchemaDefinition schema)
    {
        if (!HasExternalFields(schema))
        {
            return false;
        }

        var providesReferences = CollectProvidesReferences(schema);
        var requireReferences = CollectRequireReferences(schema);
        var removedAny = false;

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            var deadExternalFields = new List<MutableOutputFieldDefinition>();

            foreach (var field in complexType.Fields)
            {
                if (!field.Directives.ContainsName(FederationDirectiveNames.External))
                {
                    continue;
                }

                if (!providesReferences.Contains((complexType.Name, field.Name))
                    && !requireReferences.Contains((complexType.Name, field.Name)))
                {
                    deadExternalFields.Add(field);
                }
            }

            foreach (var field in deadExternalFields)
            {
                complexType.Fields.Remove(field);
                removedAny = true;
            }
        }

        return removedAny;
    }

    private static bool HasExternalFields(MutableSchemaDefinition schema)
    {
        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            foreach (var field in complexType.Fields)
            {
                if (field.Directives.ContainsName(FederationDirectiveNames.External))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsReferencedByOutputField(
        MutableSchemaDefinition schema,
        MutableObjectTypeDefinition objectType)
    {
        foreach (var type in schema.Types)
        {
            if (type is MutableUnionTypeDefinition unionType
                && unionType.Types.AsEnumerable().Any(
                    t => t.Name.Equals(objectType.Name, StringComparison.Ordinal)))
            {
                return true;
            }

            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            foreach (var field in complexType.Fields)
            {
                if (field.Type.NamedType().Name.Equals(objectType.Name, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static HashSet<(string TypeName, string FieldName)> CollectProvidesReferences(
        MutableSchemaDefinition schema)
    {
        var referenced = new HashSet<(string, string)>();

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            foreach (var field in complexType.Fields)
            {
                var providesDirective = field.Directives.FirstOrDefault(
                    FederationDirectiveNames.Provides);

                if (providesDirective is null)
                {
                    continue;
                }

                if (!providesDirective.Arguments.TryGetValue("fields", out var fieldsValue)
                    || fieldsValue is not StringValueNode fieldsString)
                {
                    continue;
                }

                SelectionSetNode selectionSet;

                try
                {
                    selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                        "{ " + fieldsString.Value + " }");
                }
                catch (SyntaxException)
                {
                    continue;
                }

                var namedType = field.Type.NamedType();

                if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                        namedType.Name, out var targetType))
                {
                    continue;
                }

                CollectReferencedFields(
                    selectionSet,
                    targetType,
                    schema,
                    referenced,
                    includeInterfaceRuntimeFields: true);
            }
        }

        return referenced;
    }

    /// <summary>
    /// Collects every field referenced by a <c>@require</c> field-selection map in the
    /// schema, including intermediate path fields and leaf fields. This is the single
    /// source of truth for "referenced by <c>@require</c>", shared with
    /// <c>ExternalUnusedRule</c>.
    /// </summary>
    internal static HashSet<(string TypeName, string FieldName)> CollectRequireReferences(
        MutableSchemaDefinition schema)
    {
        var referenced = new HashSet<(string, string)>();
        var validator = new FieldSelectionMapValidator(schema);

        foreach (var type in schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            foreach (var outputField in type.Fields)
            {
                foreach (var argument in outputField.Arguments)
                {
                    foreach (var requireDirective in argument.Directives[Require])
                    {
                        if (!requireDirective.Arguments.TryGetValue(Field, out var fieldValue)
                            || fieldValue is not StringValueNode fieldString)
                        {
                            continue;
                        }

                        IValueSelectionNode fieldSelectionMap;

                        try
                        {
                            fieldSelectionMap =
                                new FieldSelectionMapParser(fieldString.Value).Parse();
                        }
                        catch (FieldSelectionMapSyntaxException)
                        {
                            continue;
                        }

                        if (!schema.Types.ContainsName(argument.Type.AsTypeDefinition().Name)
                            || !schema.Types.ContainsName(type.Name))
                        {
                            continue;
                        }

                        var inputTypeNode = argument.Type.ToTypeNode();
                        var inputTypeDefinition =
                            schema.Types[argument.Type.AsTypeDefinition().Name];
                        var inputType = inputTypeNode.RewriteToType(inputTypeDefinition);
                        var outputTypeNode = type.ToTypeNode();
                        var outputTypeDefinition = schema.Types[type.Name];
                        var outputType = outputTypeNode.RewriteToType(outputTypeDefinition);

                        validator.Validate(
                            fieldSelectionMap,
                            inputType,
                            outputType,
                            out var selectedFields);

                        // A @require map that traverses an object- or interface-typed
                        // intermediate (a nested selection such as
                        // "dimensions { size weight }") keeps its ENTIRE subtree with
                        // @external intact: the intermediate path field has to stay
                        // traversable and its leaf fields belong to the same non-resolvable
                        // input contribution, so dropping them would leave the intermediate
                        // type empty. A map that is a flat, root-level scalar requirement
                        // (such as "price") keeps nothing: its external leaf is removed so it
                        // never competes with its real owner as a spurious partial
                        // contribution.
                        var hasComplexIntermediate = false;

                        foreach (var selectedField in selectedFields)
                        {
                            if (selectedField.Type.NamedType() is IComplexTypeDefinition)
                            {
                                hasComplexIntermediate = true;
                                break;
                            }
                        }

                        if (!hasComplexIntermediate)
                        {
                            continue;
                        }

                        foreach (var selectedField in selectedFields)
                        {
                            var coordinate = selectedField.Coordinate;

                            if (coordinate.MemberName is { } memberName)
                            {
                                referenced.Add((coordinate.Name, memberName));
                            }
                        }
                    }
                }
            }
        }

        return referenced;
    }

    internal static HashSet<(string TypeName, string FieldName)> CollectKeyReferences(
        MutableSchemaDefinition schema)
    {
        var referenced = new HashSet<(string, string)>();

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            foreach (var keyDirective in complexType.Directives[FederationDirectiveNames.Key])
            {
                if (!keyDirective.Arguments.TryGetValue("fields", out var fieldsValue)
                    || fieldsValue is not StringValueNode fieldsString)
                {
                    continue;
                }

                SelectionSetNode selectionSet;

                try
                {
                    selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                        "{ " + fieldsString.Value + " }");
                }
                catch (SyntaxException)
                {
                    continue;
                }

                CollectReferencedFields(
                    selectionSet,
                    complexType,
                    schema,
                    referenced,
                    includeInterfaceRuntimeFields: false);
            }
        }

        return referenced;
    }

    private static void CollectReferencedFields(
        SelectionSetNode selectionSet,
        MutableComplexTypeDefinition currentType,
        MutableSchemaDefinition schema,
        HashSet<(string, string)> referenced,
        bool includeInterfaceRuntimeFields)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                    referenced.Add((currentType.Name, fieldNode.Name.Value));

                    if (includeInterfaceRuntimeFields
                        && currentType is MutableInterfaceTypeDefinition interfaceType)
                    {
                        foreach (var possibleType in schema.GetPossibleTypes(interfaceType))
                        {
                            if (possibleType.Fields.ContainsName(fieldNode.Name.Value))
                            {
                                referenced.Add((possibleType.Name, fieldNode.Name.Value));
                            }
                        }
                    }

                    if (fieldNode.SelectionSet?.Selections.Count > 0
                        && currentType.Fields.TryGetField(
                            fieldNode.Name.Value, out var nestedField))
                    {
                        var nestedNamedType = nestedField.Type.NamedType();

                        if (schema.Types.TryGetType<MutableComplexTypeDefinition>(
                                nestedNamedType.Name, out var nestedType))
                        {
                            CollectReferencedFields(
                                fieldNode.SelectionSet,
                                nestedType,
                                schema,
                                referenced,
                                includeInterfaceRuntimeFields);
                        }
                    }

                    break;

                case InlineFragmentNode inlineFragment
                    when inlineFragment.TypeCondition is not null:

                    if (schema.Types.TryGetType<MutableComplexTypeDefinition>(
                            inlineFragment.TypeCondition.Name.Value, out var fragmentType))
                    {
                        CollectReferencedFields(
                            inlineFragment.SelectionSet,
                            fragmentType,
                            schema,
                            referenced,
                            includeInterfaceRuntimeFields);
                    }

                    break;
            }
        }
    }
}
