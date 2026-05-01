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

        var argument = new MutableInputFieldDefinition(fieldName, inputType)
        {
            DeclaringMember = targetField
        };

        argument.Directives.Add(
            new Directive(
                requireDef,
                new ArgumentAssignment("field", fieldName)));

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

        var hash = ComputeHash(declaringType.Name, targetField.Name, fieldName);
        var inputTypeName = $"{namedType.Name}Input_{hash}";
        var inputType = new MutableInputObjectTypeDefinition(inputTypeName);
        var addedFields = new HashSet<string>(StringComparer.Ordinal);
        var fieldMappings = new List<(string InputFieldName, string SourcePath)>();

        // When the source field is nullable, any field reached through it must also
        // be nullable on the input type since the path may yield null at runtime.
        var forceNullable = sourceField.Type is not NonNullType;

        // For list types, paths inside the list bracket are relative to each element,
        // so we pass an empty prefix. For non-list types, paths are absolute from the
        // declaring type, so we pass the field name as the prefix.
        CollectInputFields(
            fieldNode.SelectionSet!,
            nestedType,
            schema,
            inputType,
            isList ? string.Empty : fieldName,
            typeCondition: null,
            forceNullable,
            addedFields,
            fieldMappings);

        if (inputType.Fields.Count == 0)
        {
            return;
        }

        pendingInputTypes.Add(inputType);

        var fieldSelectionMap = isList
            ? BuildListFieldSelectionMap(fieldName, fieldMappings)
            : BuildFieldSelectionMap(fieldMappings);

        var argType = RebuildTypeWrapping(sourceField.Type, inputType);

        if (argType is not IInputType argInputType)
        {
            return;
        }

        var argument = new MutableInputFieldDefinition(fieldName, argInputType)
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
                                "field", $"<{typeName}>.{leafName}")));

                    targetField.Arguments.Add(argument);
                    break;
                }

                case FieldNode { SelectionSet.Selections.Count: > 0 } nestedField:
                {
                    var nestedFieldName = nestedField.Name.Value;

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
                        declaringType.Name, targetField.Name, nestedFieldName);
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
                        $"<{typeName}>.{nestedFieldName}",
                        typeCondition: null,
                        forceNullable: true,
                        addedFields,
                        fieldMappings);

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
        HashSet<string> addedFields,
        List<(string InputFieldName, string SourcePath)> fieldMappings)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode { SelectionSet: null or { Selections.Count: 0 } } leafField:
                {
                    var fieldName = leafField.Name.Value;

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

                    var actualType = forceNullable
                        ? StripNonNull(fieldType)
                        : fieldType;

                    var path = string.IsNullOrEmpty(pathPrefix)
                        ? typeCondition != null
                            ? $"<{typeCondition}>.{fieldName}"
                            : fieldName
                        : typeCondition != null
                            ? $"{pathPrefix}<{typeCondition}>.{fieldName}"
                            : $"{pathPrefix}.{fieldName}";

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
                            ? $"<{typeCondition}>.{fieldName}"
                            : fieldName
                        : typeCondition != null
                            ? $"{pathPrefix}<{typeCondition}>.{fieldName}"
                            : $"{pathPrefix}.{fieldName}";

                    CollectInputFields(
                        nestedField.SelectionSet!,
                        innerType,
                        schema,
                        inputType,
                        newPath,
                        typeCondition: null,
                        forceNullable,
                        addedFields,
                        fieldMappings);
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
                        addedFields,
                        fieldMappings);
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
        string fieldName,
        List<(string InputFieldName, string SourcePath)> fieldMappings)
    {
        var parts = fieldMappings.Select(m => $"{m.InputFieldName}: {m.SourcePath}");
        return fieldName + "[{ " + string.Join(", ", parts) + " }]";
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
