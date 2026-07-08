using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Transforms <c>@requires</c> directives into <c>@require</c> field arguments
/// per the Composite Schema specification.
/// </summary>
/// <remarks>
/// <para>
/// Flat requires (e.g., <c>@requires(fields: "price weight")</c>) produce individual
/// scalar arguments with <c>@require(field: "price")</c>.
/// </para>
/// <para>
/// Nested requires (e.g., <c>@requires(fields: "dimensions { size weight }")</c>)
/// produce a generated input object type and a single argument with a FieldSelectionMap
/// object literal: <c>@require(field: "{ size: dimensions.size, weight: dimensions.weight }")</c>.
/// </para>
/// <para>
/// Inline fragments (e.g., <c>@requires(fields: "address { ... on WorkAddress { id } }")</c>)
/// use type conditions in the FieldSelectionMap path and force fields nullable on the
/// generated input type.
/// </para>
/// </remarks>
internal static class TransformRequiresToRequire
{
    /// <summary>
    /// Applies the requires-to-require transformation on the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        var requireDef = new MutableDirectiveDefinition("require");
        var pendingInputTypes = new List<MutableInputObjectTypeDefinition>();

        // Snapshot the object types to avoid modifying the collection during iteration.
        var objectTypes = schema.Types
            .OfType<MutableObjectTypeDefinition>()
            .ToList();

        foreach (var type in objectTypes)
        {
            foreach (var field in type.Fields)
            {
                var requiresDirective = field.Directives.FirstOrDefault(
                    FederationDirectiveNames.Requires);

                if (requiresDirective is null)
                {
                    continue;
                }

                if (!requiresDirective.Arguments.TryGetValue("fields", out var fieldsValue)
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

                ProcessSelections(
                    selectionSet, type, schema, field, requireDef, pendingInputTypes);

                // Remove the @requires directive from the field.
                field.Directives.Remove(requiresDirective);
            }
        }

        // Register all generated input types after iteration is complete.
        foreach (var inputType in pendingInputTypes)
        {
            schema.Types.Add(inputType);
        }
    }

    private static void ProcessSelections(
        SelectionSetNode selectionSet,
        MutableComplexTypeDefinition declaringType,
        MutableSchemaDefinition schema,
        MutableOutputFieldDefinition targetField,
        MutableDirectiveDefinition requireDef,
        List<MutableInputObjectTypeDefinition> pendingInputTypes)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode { SelectionSet.Selections.Count: > 0 } fieldNode:
                    AddInputObjectArgument(
                        fieldNode, declaringType, schema, targetField,
                        requireDef, pendingInputTypes);
                    break;

                case FieldNode fieldNode:
                    AddScalarArgument(
                        fieldNode, declaringType, targetField, requireDef);
                    break;

                case InlineFragmentNode { TypeCondition: not null } fragment:
                    AddFragmentArguments(
                        fragment, declaringType, schema, targetField,
                        requireDef, pendingInputTypes);
                    break;
            }
        }
    }

    /// <summary>
    /// Creates a scalar argument for a leaf field in the requires selection.
    /// </summary>
    private static void AddScalarArgument(
        FieldNode fieldNode,
        MutableComplexTypeDefinition declaringType,
        MutableOutputFieldDefinition targetField,
        MutableDirectiveDefinition requireDef)
    {
        var fieldName = fieldNode.Name.Value;

        if (!declaringType.Fields.TryGetField(fieldName, out var sourceField))
        {
            return;
        }

        if (sourceField.Type is not IInputType inputType)
        {
            return;
        }

        // @require arguments are gateway-supplied and stripped from the public schema, so they are
        // generated nullable. A non-null additional argument would also violate the interface
        // argument rules for the field it is added to.
        var argument = new MutableInputFieldDefinition(fieldName, StripNonNull(inputType))
        {
            DeclaringMember = targetField
        };

        argument.Directives.Add(
            new Directive(
                requireDef,
                new ArgumentAssignment("field", FormatFieldSelection(fieldNode))));

        targetField.Arguments.Add(argument);
    }

    /// <summary>
    /// Creates an input object type and argument for a nested field in the requires
    /// selection. The generated input type is named with a hash suffix for uniqueness
    /// and is stripped by composition since it is only referenced by <c>@require</c>
    /// arguments.
    /// </summary>
    private static void AddInputObjectArgument(
        FieldNode fieldNode,
        MutableComplexTypeDefinition declaringType,
        MutableSchemaDefinition schema,
        MutableOutputFieldDefinition targetField,
        MutableDirectiveDefinition requireDef,
        List<MutableInputObjectTypeDefinition> pendingInputTypes)
    {
        var fieldName = fieldNode.Name.Value;

        if (!declaringType.Fields.TryGetField(fieldName, out var sourceField))
        {
            return;
        }

        var namedType = sourceField.Type.NamedType();

        if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                namedType.Name, out var nestedType))
        {
            return;
        }

        // Detect whether the source field is a list type (e.g. [Product] or [Product!]!).
        var isList = sourceField.Type.NullableType() is ListType;

        var fieldSelection = FormatFieldSelection(fieldNode);
        var hash = ComputeHash(declaringType.Name, targetField.Name, fieldSelection);
        var inputTypeName = $"{namedType.Name}Input_{hash}";
        var inputType = new MutableInputObjectTypeDefinition(inputTypeName);
        var addedFields = new HashSet<string>(StringComparer.Ordinal);
        var fieldMappings = new List<(string InputFieldName, string SourcePath)>();

        // When the source field is nullable, any field reached through it must also
        // be nullable on the input type since the path may yield null at runtime.
        var forceNullable = sourceField.Type is not NonNullType;

        // For list types, paths inside the list bracket are relative to each element,
        // so we pass an empty prefix. For non-list types, paths are absolute from the
        // declaring type, so we pass the field selection as the prefix.
        CollectInputFields(
            fieldNode.SelectionSet!,
            nestedType,
            schema,
            inputType,
            isList ? string.Empty : fieldSelection,
            typeCondition: null,
            forceNullable,
            pathIsExternal: IsExternal(sourceField),
            addedFields,
            fieldMappings,
            pendingInputTypes,
            inputTypeName);

        if (inputType.Fields.Count == 0)
        {
            return;
        }

        pendingInputTypes.Add(inputType);

        var fieldSelectionMap = isList
            ? BuildListFieldSelectionMap(fieldSelection, fieldMappings)
            : BuildFieldSelectionMap(fieldMappings);

        var argType = RebuildTypeWrapping(sourceField.Type, inputType);

        if (argType is not IInputType argInputType)
        {
            return;
        }

        // @require arguments are gateway-supplied and stripped from the public schema, so they are
        // generated nullable. A non-null additional argument would also violate the interface
        // argument rules for the field it is added to.
        var argument = new MutableInputFieldDefinition(fieldName, StripNonNull(argInputType))
        {
            DeclaringMember = targetField
        };

        argument.Directives.Add(
            new Directive(
                requireDef,
                new ArgumentAssignment("field", fieldSelectionMap)));

        targetField.Arguments.Add(argument);
    }

    /// <summary>
    /// Handles top-level inline fragments in the requires selection by creating
    /// scalar arguments with type-conditioned paths, forced nullable.
    /// </summary>
    private static void AddFragmentArguments(
        InlineFragmentNode fragment,
        MutableComplexTypeDefinition declaringType,
        MutableSchemaDefinition schema,
        MutableOutputFieldDefinition targetField,
        MutableDirectiveDefinition requireDef,
        List<MutableInputObjectTypeDefinition> pendingInputTypes)
    {
        var typeName = fragment.TypeCondition!.Name.Value;

        if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                typeName, out var concreteType))
        {
            return;
        }

        foreach (var selection in fragment.SelectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode { SelectionSet: null or { Selections.Count: 0 } } leafField:
                {
                    var leafName = leafField.Name.Value;

                    if (!concreteType.Fields.TryGetField(leafName, out var sourceField))
                    {
                        break;
                    }

                    if (sourceField.Type is not IInputType inputType)
                    {
                        break;
                    }

                    var argument = new MutableInputFieldDefinition(
                        leafName, StripNonNull(inputType))
                    {
                        DeclaringMember = targetField
                    };

                    argument.Directives.Add(
                        new Directive(
                            requireDef,
                            new ArgumentAssignment(
                                "field", $"<{typeName}>.{FormatFieldSelection(leafField)}")));

                    targetField.Arguments.Add(argument);
                    break;
                }

                case FieldNode { SelectionSet.Selections.Count: > 0 } nestedField:
                {
                    var nestedFieldName = nestedField.Name.Value;
                    var nestedFieldSelection = FormatFieldSelection(nestedField);

                    if (!concreteType.Fields.TryGetField(
                            nestedFieldName, out var sourceField))
                    {
                        break;
                    }

                    var innerNamedType = sourceField.Type.NamedType();

                    if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                            innerNamedType.Name, out var innerType))
                    {
                        break;
                    }

                    var hash = ComputeHash(
                        declaringType.Name, targetField.Name, nestedFieldSelection);
                    var inputTypeName = $"{innerNamedType.Name}Input_{hash}";
                    var inputObj = new MutableInputObjectTypeDefinition(inputTypeName);
                    var addedFields = new HashSet<string>(StringComparer.Ordinal);
                    var fieldMappings =
                        new List<(string InputFieldName, string SourcePath)>();

                    CollectInputFields(
                        nestedField.SelectionSet!,
                        innerType,
                        schema,
                        inputObj,
                        $"<{typeName}>.{nestedFieldSelection}",
                        typeCondition: null,
                        forceNullable: true,
                        pathIsExternal: IsExternal(sourceField),
                        addedFields,
                        fieldMappings,
                        pendingInputTypes,
                        inputTypeName);

                    if (inputObj.Fields.Count == 0)
                    {
                        break;
                    }

                    pendingInputTypes.Add(inputObj);

                    var fieldMap = BuildFieldSelectionMap(fieldMappings);

                    var argument = new MutableInputFieldDefinition(
                        nestedFieldName, (IInputType)(IType)inputObj)
                    {
                        DeclaringMember = targetField
                    };

                    argument.Directives.Add(
                        new Directive(
                            requireDef,
                            new ArgumentAssignment("field", fieldMap)));

                    targetField.Arguments.Add(argument);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Recursively walks a selection set to populate an input object type with fields
    /// and collect FieldSelectionMap path mappings.
    /// </summary>
    private static void CollectInputFields(
        SelectionSetNode selectionSet,
        MutableComplexTypeDefinition currentType,
        MutableSchemaDefinition schema,
        MutableInputObjectTypeDefinition inputType,
        string pathPrefix,
        string? typeCondition,
        bool forceNullable,
        bool pathIsExternal,
        HashSet<string> addedFields,
        List<(string InputFieldName, string SourcePath)> fieldMappings,
        List<MutableInputObjectTypeDefinition> pendingInputTypes,
        string parentInputName)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode { SelectionSet: null or { Selections.Count: 0 } } leafField:
                {
                    var fieldName = leafField.Name.Value;
                    var fieldSelection = FormatFieldSelection(leafField);

                    if (addedFields.Contains(fieldName))
                    {
                        break;
                    }

                    if (!currentType.Fields.TryGetField(
                            fieldName, out var sourceField))
                    {
                        break;
                    }

                    if (sourceField.Type is not IInputType fieldType)
                    {
                        break;
                    }

                    if (pathIsExternal
                        && currentType is MutableObjectTypeDefinition
                        && !IsKeyField(currentType, fieldName))
                    {
                        ApplyExternalDirective(sourceField);
                    }

                    var actualType = forceNullable
                        ? StripNonNull(fieldType)
                        : fieldType;

                    var path = string.IsNullOrEmpty(pathPrefix)
                        ? typeCondition != null
                            ? $"<{typeCondition}>.{fieldSelection}"
                            : fieldSelection
                        : typeCondition != null
                            ? $"{pathPrefix}<{typeCondition}>.{fieldSelection}"
                            : $"{pathPrefix}.{fieldSelection}";

                    var inputField = new MutableInputFieldDefinition(
                        fieldName, actualType)
                    {
                        DeclaringMember = inputType
                    };

                    inputType.Fields.Add(inputField);
                    addedFields.Add(fieldName);
                    fieldMappings.Add((fieldName, path));
                    break;
                }

                case FieldNode { SelectionSet.Selections.Count: > 0 } nestedField:
                {
                    var fieldName = nestedField.Name.Value;
                    var fieldSelection = FormatFieldSelection(nestedField);

                    if (!currentType.Fields.TryGetField(
                            fieldName, out var pathField))
                    {
                        break;
                    }

                    var innerNamedType = pathField.Type.NamedType();

                    if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                            innerNamedType.Name, out var innerType))
                    {
                        break;
                    }

                    var newPath = string.IsNullOrEmpty(pathPrefix)
                        ? typeCondition != null
                            ? $"<{typeCondition}>.{fieldSelection}"
                            : fieldSelection
                        : typeCondition != null
                            ? $"{pathPrefix}<{typeCondition}>.{fieldSelection}"
                            : $"{pathPrefix}.{fieldSelection}";

                    // A list-typed intermediate cannot be flattened into a dotted path: the
                    // FieldSelectionMap has to map over the list with `path[{ ... }]` syntax, and
                    // the input field becomes a list of a nested input object whose paths are
                    // relative to each element. A non-list intermediate keeps flattening into the
                    // current input object.
                    if (pathField.Type.NullableType() is ListType)
                    {
                        if (!addedFields.Add(fieldName))
                        {
                            break;
                        }

                        var elementInputName =
                            $"{innerNamedType.Name}Input_"
                            + ComputeHash(parentInputName, newPath, fieldName);
                        var elementInput =
                            new MutableInputObjectTypeDefinition(elementInputName);
                        var elementAddedFields = new HashSet<string>(StringComparer.Ordinal);
                        var elementMappings =
                            new List<(string InputFieldName, string SourcePath)>();

                        CollectInputFields(
                            nestedField.SelectionSet!,
                            innerType,
                            schema,
                            elementInput,
                            pathPrefix: string.Empty,
                            typeCondition: null,
                            forceNullable,
                            pathIsExternal || IsExternal(pathField),
                            elementAddedFields,
                            elementMappings,
                            pendingInputTypes,
                            elementInputName);

                        if (elementInput.Fields.Count == 0)
                        {
                            break;
                        }

                        pendingInputTypes.Add(elementInput);

                        inputType.Fields.Add(
                            new MutableInputFieldDefinition(
                                fieldName, new ListType(elementInput))
                            {
                                DeclaringMember = inputType
                            });

                        var innerMap = string.Join(
                            ", ",
                            elementMappings.Select(m => $"{m.InputFieldName}: {m.SourcePath}"));
                        fieldMappings.Add((fieldName, $"{newPath}[{{ {innerMap} }}]"));
                        break;
                    }

                    CollectInputFields(
                        nestedField.SelectionSet!,
                        innerType,
                        schema,
                        inputType,
                        newPath,
                        typeCondition: null,
                        forceNullable,
                        pathIsExternal || IsExternal(pathField),
                        addedFields,
                        fieldMappings,
                        pendingInputTypes,
                        parentInputName);
                    break;
                }

                case InlineFragmentNode { TypeCondition: not null } fragment:
                    var fragmentTypeName = fragment.TypeCondition.Name.Value;

                    if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                            fragmentTypeName, out var concreteType))
                    {
                        break;
                    }

                    CollectInputFields(
                        fragment.SelectionSet,
                        concreteType,
                        schema,
                        inputType,
                        pathPrefix,
                        fragmentTypeName,
                        forceNullable: true,
                        pathIsExternal,
                        addedFields,
                        fieldMappings,
                        pendingInputTypes,
                        parentInputName);
                    break;
            }
        }
    }

    private static string BuildFieldSelectionMap(
        List<(string InputFieldName, string SourcePath)> fieldMappings)
    {
        var parts = fieldMappings.Select(m => $"{m.InputFieldName}: {m.SourcePath}");
        return "{ " + string.Join(", ", parts) + " }";
    }

    /// <summary>
    /// Builds a FieldSelectionMap with list syntax for list-typed requires fields.
    /// For example: <c>similar[{ id: id }]</c>
    /// </summary>
    private static string BuildListFieldSelectionMap(
        string fieldSelection,
        List<(string InputFieldName, string SourcePath)> fieldMappings)
    {
        var parts = fieldMappings.Select(m => $"{m.InputFieldName}: {m.SourcePath}");
        return fieldSelection + "[{ " + string.Join(", ", parts) + " }]";
    }

    private static string FormatFieldSelection(FieldNode fieldNode)
    {
        if (fieldNode.Arguments.Count == 0)
        {
            return fieldNode.Name.Value;
        }

        var arguments = fieldNode.Arguments.Select(a => a.ToString(indented: false));

        return $"{fieldNode.Name.Value}({string.Join(", ", arguments)})";
    }

    private static bool IsExternal(MutableOutputFieldDefinition field)
        => field.Directives.ContainsName(FederationDirectiveNames.External);

    private static void ApplyExternalDirective(MutableOutputFieldDefinition field)
    {
        if (!IsExternal(field))
        {
            var externalDirectiveDefinition =
                new MutableDirectiveDefinition(FederationDirectiveNames.External);

            field.Directives.Add(new Directive(externalDirectiveDefinition));
        }
    }

    private static bool IsKeyField(
        MutableComplexTypeDefinition type,
        string fieldName)
    {
        foreach (var keyDirective in type.GetKeyDirectives())
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

            foreach (var selection in selectionSet.Selections)
            {
                if (selection is FieldNode keyField
                    && keyField.Name.Value.Equals(fieldName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IInputType StripNonNull(IInputType type)
    {
        if (type is NonNullType nonNull
            && nonNull.NullableType is IInputType nullable)
        {
            return nullable;
        }

        return type;
    }

    /// <summary>
    /// Rebuilds the type wrapping (NonNull, List) from the source type around the
    /// replacement named type. For example, if the source is [Product!]! and the
    /// replacement is ProductInput_hash, the result is [ProductInput_hash!]!.
    /// </summary>
    private static IType RebuildTypeWrapping(
        IType sourceType, MutableInputObjectTypeDefinition replacement)
    {
        return Rebuild(sourceType);

        IType Rebuild(IType type)
        {
            return type switch
            {
                NonNullType nonNull => new NonNullType(Rebuild(nonNull.NullableType)),
                ListType list => new ListType(Rebuild(list.ElementType)),
                _ => replacement
            };
        }
    }

    private static uint ComputeHash(
        string declaringType, string fieldName, string argName)
    {
        unchecked
        {
            var hash = 2166136261u;

            foreach (var c in declaringType)
            {
                hash = (hash ^ c) * 16777619u;
            }

            hash ^= 31u;

            foreach (var c in fieldName)
            {
                hash = (hash ^ c) * 16777619u;
            }

            hash ^= 31u;

            foreach (var c in argName)
            {
                hash = (hash ^ c) * 16777619u;
            }

            return hash;
        }
    }
}
