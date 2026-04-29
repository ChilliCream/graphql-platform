using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Generates <c>@lookup</c> query fields for each resolvable entity key.
/// <para>
/// Flat scalar keys (<c>@key(fields: "id")</c>, <c>@key(fields: "sku package")</c>)
/// produce a lookup field whose arguments mirror the key fields one-to-one.
/// </para>
/// <para>
/// Nested object and list keys (<c>@key(fields: "metadata { id }")</c>,
/// <c>@key(fields: "products { id }")</c>,
/// <c>@key(fields: "products { id pid category { id tag } } selected { id }")</c>)
/// produce a lookup field with a single <c>key</c> argument of a freshly
/// generated input object type. The input type exposes one field per top-level
/// segment of the <c>@key</c> selection and mirrors the entity-representation
/// shape so the Apollo Federation connector can copy the variable value
/// straight into the <c>_entities(representations: ...)</c> payload.
/// </para>
/// </summary>
internal static class GenerateLookupFields
{
    private const string NestedLookupArgumentName = "key";

    /// <summary>
    /// Applies the lookup field generation to the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        if (schema.QueryType is null)
        {
            return;
        }

        var internalDef = new MutableDirectiveDefinition("internal");
        var lookupDef = new MutableDirectiveDefinition("lookup");
        var isDef = new MutableDirectiveDefinition("is");
        var shareableDef = new MutableDirectiveDefinition("shareable");

        // Generated input object types are cached by name so that repeated
        // references to the same nested shape share a single input type.
        var inputTypeCache = new Dictionary<string, MutableInputObjectTypeDefinition>(
            StringComparer.Ordinal);

        // Snapshot the owning types up front: generating lookup fields for
        // nested keys appends new input object types to 'schema.Types', which
        // would otherwise invalidate an in-flight enumerator.
        var complexTypes = schema.Types
            .OfType<MutableComplexTypeDefinition>()
            .ToArray();

        foreach (var complexType in complexTypes)
        {
            // Keys whose selection set references a list-typed field on the
            // owner (e.g. '@key(fields: "products { id }")' where 'products'
            // is '[Product!]!') are surfaced in the composite schema purely
            // via the generated '@lookup' field + '@is' metadata. The '@key'
            // directive itself is dropped from the type because the Composite
            // Schema Spec disallows list, interface, or union top-level fields
            // in '@key' selections.
            var keysToRemove = new List<Directive>();
            var fieldsToMarkShareable = new HashSet<string>(StringComparer.Ordinal);

            foreach (var keyDirective in complexType.Directives["key"])
            {
                if (!keyDirective.Arguments.TryGetValue("fields", out var fieldsValue)
                    || fieldsValue is not StringValueNode fieldsString)
                {
                    continue;
                }

                var resolvable = true;

                if (keyDirective.Arguments.TryGetValue("resolvable", out var resolvableValue)
                    && resolvableValue is BooleanValueNode boolValue)
                {
                    resolvable = boolValue.Value;
                }

                if (!resolvable)
                {
                    continue;
                }

                var result = GenerateLookupField(
                    schema,
                    complexType,
                    fieldsString.Value,
                    internalDef,
                    lookupDef,
                    isDef,
                    inputTypeCache);

                if (result is null)
                {
                    continue;
                }

                schema.QueryType.Fields.Add(result.Field);

                if (result.KeyHasListSegment)
                {
                    keysToRemove.Add(keyDirective);

                    foreach (var fieldName in result.TopLevelKeyFieldNames)
                    {
                        fieldsToMarkShareable.Add(fieldName);
                    }
                }
            }

            foreach (var directive in keysToRemove)
            {
                complexType.Directives.Remove(directive);
            }

            // Apollo Federation treats fields referenced by a '@key' directive
            // as implicitly shareable. Once the list-typed '@key' is dropped
            // we must surface that sharing intent explicitly so the Composite
            // Schema Spec's field-sharing rule does not reject the field
            // being produced by multiple source schemas.
            if (fieldsToMarkShareable.Count > 0)
            {
                foreach (var fieldName in fieldsToMarkShareable)
                {
                    if (complexType.Fields.TryGetField(fieldName, out var keyField)
                        && !keyField.Directives.ContainsName("shareable"))
                    {
                        keyField.Directives.Add(new Directive(shareableDef));
                    }
                }
            }
        }
    }

    private static GenerateLookupFieldResult? GenerateLookupField(
        MutableSchemaDefinition schema,
        MutableComplexTypeDefinition complexType,
        string fieldsSelection,
        MutableDirectiveDefinition internalDef,
        MutableDirectiveDefinition lookupDef,
        MutableDirectiveDefinition isDef,
        Dictionary<string, MutableInputObjectTypeDefinition> inputTypeCache)
    {
        SelectionSetNode selectionSet;

        try
        {
            selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                "{ " + fieldsSelection + " }");
        }
        catch (SyntaxException)
        {
            return null;
        }

        var topLevelFieldNames = new List<string>();
        var hasNestedSegment = false;
        var keyHasListSegment = false;

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode topLevelField)
            {
                continue;
            }

            var keyFieldName = topLevelField.Name.Value;

            if (!complexType.Fields.TryGetField(keyFieldName, out var ownerField))
            {
                return null;
            }

            topLevelFieldNames.Add(keyFieldName);

            if (topLevelField.SelectionSet is null
                || topLevelField.SelectionSet.Selections.Count == 0)
            {
                continue;
            }

            hasNestedSegment = true;

            if (IsListType(ownerField.Type))
            {
                keyHasListSegment = true;
            }
        }

        if (topLevelFieldNames.Count == 0)
        {
            return null;
        }

        // Build a temporary field to set as DeclaringMember on arguments.
        var lookupField = new MutableOutputFieldDefinition(
            "placeholder",
            complexType)
        {
            DeclaringMember = schema.QueryType
        };

        var fieldName = hasNestedSegment
            ? BuildNestedLookupField(
                schema,
                complexType,
                selectionSet,
                lookupField,
                isDef,
                inputTypeCache)
            : BuildFlatLookupField(complexType, selectionSet, lookupField);

        if (fieldName is null)
        {
            return null;
        }

        lookupField.Name = fieldName;
        lookupField.Directives.Add(new Directive(internalDef));
        lookupField.Directives.Add(new Directive(lookupDef));

        return new GenerateLookupFieldResult(lookupField, keyHasListSegment, topLevelFieldNames);
    }

    private static string? BuildFlatLookupField(
        MutableComplexTypeDefinition complexType,
        SelectionSetNode selectionSet,
        MutableOutputFieldDefinition lookupField)
    {
        var nameParts = new List<string>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode topLevelField)
            {
                continue;
            }

            var keyFieldName = topLevelField.Name.Value;

            if (!complexType.Fields.TryGetField(keyFieldName, out var ownerField))
            {
                return null;
            }

            if (ownerField.Type is not IInputType inputType)
            {
                continue;
            }

            var scalarArgumentType = EnsureNonNull(inputType);

            if (scalarArgumentType is not IInputType nonNullScalar)
            {
                continue;
            }

            var scalarArgument = new MutableInputFieldDefinition(keyFieldName, nonNullScalar)
            {
                DeclaringMember = lookupField
            };

            lookupField.Arguments.Add(scalarArgument);
            nameParts.Add(ToPascalCase(keyFieldName));
        }

        if (lookupField.Arguments.Count == 0)
        {
            return null;
        }

        return ToCamelCase(complexType.Name) + "By" + string.Join("And", nameParts);
    }

    private static string? BuildNestedLookupField(
        MutableSchemaDefinition schema,
        MutableComplexTypeDefinition complexType,
        SelectionSetNode selectionSet,
        MutableOutputFieldDefinition lookupField,
        MutableDirectiveDefinition isDef,
        Dictionary<string, MutableInputObjectTypeDefinition> inputTypeCache)
    {
        // For nested/list keys we collapse the whole '@key' selection into a
        // single input object type whose shape mirrors the Apollo Federation
        // representation root. The connector then copies the variable value
        // directly into the '_entities' representation (under the entity
        // '__typename'), without needing to split across multiple arguments.
        // The input type name encodes the full selection shape so that two
        // source schemas with different sub-selections on the same top-level
        // path generate distinct input types that survive composition merge.
        var wrapperBaseName = complexType.Name
            + "By"
            + BuildSelectionFingerprint(selectionSet)
            + "Input";

        var wrapperInput = BuildInputTypeFromSelection(
            schema,
            complexType,
            selectionSet,
            wrapperBaseName,
            inputTypeCache);

        if (wrapperInput is null)
        {
            return null;
        }

        IType argumentType = new NonNullType(wrapperInput);

        if (argumentType is not IInputType argumentInputType)
        {
            return null;
        }

        var keyArgument = new MutableInputFieldDefinition(NestedLookupArgumentName, argumentInputType)
        {
            DeclaringMember = lookupField
        };

        // Emit '@is(field: "{ ... }")' in Fusion FSM so downstream composer
        // stages can discover the key shape. The expression describes how each
        // top-level input field populates the corresponding output path on
        // the entity type.
        var fsm = BuildRootFieldSelectionMap(complexType, selectionSet);

        keyArgument.Directives.Add(
            new Directive(isDef, new ArgumentAssignment("field", fsm)));

        lookupField.Arguments.Add(keyArgument);

        return ToCamelCase(complexType.Name) + "By" + BuildSelectionFingerprint(selectionSet);
    }

    private static MutableInputObjectTypeDefinition? BuildInputTypeFromSelection(
        MutableSchemaDefinition schema,
        ITypeDefinition ownerType,
        SelectionSetNode selectionSet,
        string baseName,
        Dictionary<string, MutableInputObjectTypeDefinition> inputTypeCache)
    {
        if (ownerType is not MutableComplexTypeDefinition ownerComplex)
        {
            return null;
        }

        var inputType = new MutableInputObjectTypeDefinition(baseName);

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode fieldNode)
            {
                continue;
            }

            var fieldName = fieldNode.Name.Value;

            if (!ownerComplex.Fields.TryGetField(fieldName, out var childField))
            {
                return null;
            }

            IInputType inputFieldType;

            if (fieldNode.SelectionSet is null
                || fieldNode.SelectionSet.Selections.Count == 0)
            {
                // Leaf scalar inside the nested selection.
                if (childField.Type is not IInputType leafInputType)
                {
                    return null;
                }

                inputFieldType = leafInputType;
            }
            else
            {
                // Deeper nested object/list: recurse into another input type.
                var childIsList = IsListType(childField.Type);
                var nestedBaseName = baseName + "_" + ToPascalCase(fieldName);

                var nestedInput = BuildInputTypeFromSelection(
                    schema,
                    childField.Type.NamedType(),
                    fieldNode.SelectionSet,
                    nestedBaseName,
                    inputTypeCache);

                if (nestedInput is null)
                {
                    return null;
                }

                IType nestedType = nestedInput;

                if (childIsList)
                {
                    nestedType = new NonNullType(new ListType(new NonNullType(nestedType)));
                }

                if (nestedType is not IInputType nestedInputFieldType)
                {
                    return null;
                }

                inputFieldType = nestedInputFieldType;
            }

            var inputField = new MutableInputFieldDefinition(fieldName, inputFieldType)
            {
                DeclaringMember = inputType
            };

            inputType.Fields.Add(inputField);
        }

        if (inputType.Fields.Count == 0)
        {
            return null;
        }

        // Deduplicate: an identically shaped input type may already have been
        // generated for a prior key that referenced the same owner type.
        if (inputTypeCache.TryGetValue(inputType.Name, out var existing))
        {
            return existing;
        }

        inputTypeCache[inputType.Name] = inputType;
        schema.Types.Add(inputType);

        return inputType;
    }

    private static bool IsListType(IType type)
    {
        while (true)
        {
            switch (type)
            {
                case ListType:
                    return true;
                case NonNullType nonNull:
                    type = nonNull.NullableType;
                    continue;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Builds a PascalCase fingerprint of a key's selection set so that two
    /// source schemas with divergent sub-selections on the same top-level path
    /// generate distinct, non-colliding input type names.
    /// </summary>
    private static string BuildSelectionFingerprint(SelectionSetNode selectionSet)
    {
        var parts = new List<string>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode fieldNode)
            {
                continue;
            }

            parts.Add(ToPascalCase(fieldNode.Name.Value));

            if (fieldNode.SelectionSet?.Selections.Count > 0)
            {
                parts.Add(BuildSelectionFingerprint(fieldNode.SelectionSet));
            }
        }

        return string.Join("And", parts);
    }

    /// <summary>
    /// Builds the '@is(field: "{ ... }")' expression for a nested/list lookup
    /// argument. Each top-level selection in the '@key' is emitted as an
    /// object-field in Fusion FSM syntax that names the entity-side field and
    /// the value-selection that derives it from the input.
    /// </summary>
    private static string BuildRootFieldSelectionMap(
        MutableComplexTypeDefinition complexType,
        SelectionSetNode selectionSet)
    {
        var parts = new List<string>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode topLevelField)
            {
                continue;
            }

            var keyFieldName = topLevelField.Name.Value;

            if (!complexType.Fields.TryGetField(keyFieldName, out var ownerField))
            {
                continue;
            }

            if (topLevelField.SelectionSet is null
                || topLevelField.SelectionSet.Selections.Count == 0)
            {
                parts.Add(keyFieldName);
                continue;
            }

            var innerBody = BuildInnerObjectBody(topLevelField.SelectionSet);
            var ownerIsList = IsListType(ownerField.Type);

            var valueExpression = ownerIsList
                ? $"{keyFieldName}[{{ {innerBody} }}]"
                : $"{keyFieldName}.{{ {innerBody} }}";

            parts.Add($"{keyFieldName}: {valueExpression}");
        }

        return "{ " + string.Join(", ", parts) + " }";
    }

    /// <summary>
    /// Renders the contents of the '{ ... }' object selection for a Fusion
    /// field-selection-map value. Leaf fields are emitted as bare names; nested
    /// object selections use <c>name: path.{ ... }</c>; nested list selections
    /// currently also use <c>name: path.{ ... }</c> since we cannot resolve the
    /// child field's list-ness from a <see cref="SelectionSetNode"/> alone at
    /// deeper levels.
    /// </summary>
    private static string BuildInnerObjectBody(SelectionSetNode selectionSet)
    {
        var parts = new List<string>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode fieldNode)
            {
                continue;
            }

            var fieldName = fieldNode.Name.Value;

            if (fieldNode.SelectionSet is null
                || fieldNode.SelectionSet.Selections.Count == 0)
            {
                parts.Add(fieldName);
                continue;
            }

            var innerBody = BuildInnerObjectBody(fieldNode.SelectionSet);
            parts.Add($"{fieldName}: {fieldName}.{{ {innerBody} }}");
        }

        return string.Join(", ", parts);
    }

    private static IType EnsureNonNull(IType type)
    {
        if (type.Kind is TypeKind.NonNull)
        {
            return type;
        }

        return new NonNullType(type);
    }

    private static string ToCamelCase(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        if (char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static string ToPascalCase(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        if (char.IsUpper(value[0]))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private sealed record GenerateLookupFieldResult(
        MutableOutputFieldDefinition Field,
        bool KeyHasListSegment,
        IReadOnlyList<string> TopLevelKeyFieldNames);
}
