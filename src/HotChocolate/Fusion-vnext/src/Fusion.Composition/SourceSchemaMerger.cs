using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.SchemaVisitors;
using HotChocolate.Fusion.SyntaxRewriters;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Language.Visitors;
using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.StringUtilities;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;
using TypeNames = HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaMerger
{
    private static readonly RemoveDirectiveNodesSyntaxRewriter RemoveDirectivesRewriter = new();
    private readonly ImmutableSortedSet<SchemaDefinition> _schemas;
    private readonly FrozenDictionary<SchemaDefinition, string> _schemaConstantNames;
    private readonly SourceSchemaMergerOptions _options;
    private readonly FrozenDictionary<string, INamedTypeDefinition> _fusionTypeDefinitions;
    private readonly FrozenDictionary<string, DirectiveDefinition> _fusionDirectiveDefinitions;

    public SourceSchemaMerger(
        ImmutableSortedSet<SchemaDefinition> schemas,
        SourceSchemaMergerOptions? options = null)
    {
        _schemas = schemas;
        _schemaConstantNames = schemas.ToFrozenDictionary(s => s, s => ToConstantCase(s.Name));
        _options = options ?? new SourceSchemaMergerOptions();
        _fusionTypeDefinitions = CreateFusionTypeDefinitions();
        _fusionDirectiveDefinitions = CreateFusionDirectiveDefinitions();
    }

    public CompositionResult<SchemaDefinition> Merge()
    {
        var mergedSchema = new SchemaDefinition();

        // [TypeName: [{Type, Schema}, ...], ...].
        var typeGroupByName = _schemas
            .SelectMany(s => s.Types, (schema, type) => new TypeInfo(type, schema))
            .OrderBy(i => i.Type.Kind) // Ensure that object types are merged before union types.
            .GroupBy(i => i.Type.Name);

        // Merge types.
        foreach (var grouping in typeGroupByName)
        {
            var mergedType = MergeTypes([.. grouping], mergedSchema);

            if (mergedType is not null)
            {
                mergedSchema.Types.Add(mergedType);
            }
        }

        SetOperationTypes(mergedSchema);

        // Add lookup directives.
        foreach (var schema in _schemas)
        {
            var discoverLookups = new DiscoverLookupsSchemaVisitor(schema);

            // [TypeName: [{LookupField, Path, Schema}, ...], ...].
            var lookupFieldGroupByTypeName = discoverLookups.Discover();

            foreach (var (typeName, lookupFieldGroup) in lookupFieldGroupByTypeName)
            {
                if (mergedSchema.Types.TryGetType(typeName, out var mergedType))
                {
                    AddFusionLookupDirectives(mergedType, [.. lookupFieldGroup]);
                }
            }
        }

        // Add Fusion definitions.
        if (_options.AddFusionDefinitions)
        {
            AddFusionDefinitions(mergedSchema);
        }

        return mergedSchema;
    }

    private INamedTypeDefinition? MergeTypes(
        ImmutableArray<TypeInfo> typeGroup,
        SchemaDefinition mergedSchema)
    {
        var kind = typeGroup[0].Type.Kind;

        Assert(typeGroup.All(i => i.Type.Kind == kind));

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return kind switch
        {
            TypeKind.Enum => MergeEnumTypes(typeGroup),
            TypeKind.InputObject => MergeInputTypes(typeGroup),
            TypeKind.Interface => MergeInterfaceTypes(typeGroup),
            TypeKind.Object => MergeObjectTypes(typeGroup),
            TypeKind.Scalar => MergeScalarTypes(typeGroup),
            TypeKind.Union => MergeUnionTypes(typeGroup, mergedSchema),
            _ => throw new InvalidOperationException()
        };
    }

    /// <summary>
    /// Takes two arguments with the same name but possibly differing in type, description, or
    /// default value, and returns a single, unified argument definition.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Argument">
    /// Specification
    /// </seealso>
    private static InputFieldDefinition MergeArguments(
        InputFieldDefinition argumentA,
        InputFieldDefinition argumentB)
    {
        var typeA = argumentA.Type;
        var typeB = argumentB.Type;
        var type = MostRestrictiveType(typeA, typeB);
        var description = argumentA.Description ?? argumentB.Description;
        var defaultValue = argumentA.DefaultValue ?? argumentB.DefaultValue;

        return new InputFieldDefinition(argumentA.Name, type)
        {
            DefaultValue = defaultValue,
            Description = description
        };
    }

    /// <summary>
    /// Merges multiple arguments that share the same name across different field definitions into a
    /// single composed argument definition.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Argument-Definitions">
    /// Specification
    /// </seealso>
    private InputFieldDefinition? MergeArgumentDefinitions(
        ImmutableArray<FieldArgumentInfo> argumentGroup)
    {
        // Remove all arguments marked with @require.
        argumentGroup = [.. argumentGroup.Where(i => !i.Argument.HasRequireDirective())];

        var mergedArgument = argumentGroup.Select(i => i.Argument).FirstOrDefault();

        if (mergedArgument is null)
        {
            return null;
        }

        foreach (var argumentInfo in argumentGroup)
        {
            mergedArgument = MergeArguments(mergedArgument, argumentInfo.Argument);
        }

        if (argumentGroup.Any(i => i.Argument.HasInaccessibleDirective()))
        {
            mergedArgument.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionInputFieldDirectives(mergedArgument, argumentGroup);

        return mergedArgument;
    }

    /// <summary>
    /// Consolidates multiple enum definitions (all sharing the <i>same name</i>) into one final
    /// enum type.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Enum-Types">
    /// Specification
    /// </seealso>
    private EnumTypeDefinition MergeEnumTypes(ImmutableArray<TypeInfo> typeGroup)
    {
        var firstEnum = typeGroup[0].Type;
        var typeName = firstEnum.Name;
        var description = firstEnum.Description;

        foreach (var typeInfo in typeGroup.Skip(1))
        {
            description ??= typeInfo.Type.Description;
        }

        var enumType = new EnumTypeDefinition(typeName) { Description = description };

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            enumType.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionTypeDirectives(enumType, typeGroup);

        // [EnumValueName: [{EnumValue, EnumType, Schema}, ...], ...].
        var enumValueGroupByName = typeGroup
            .SelectMany(
                i => ((EnumTypeDefinition)i.Type).Values,
                (i, v) => new EnumValueInfo(v, (EnumTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.EnumValue.Name);

        foreach (var grouping in enumValueGroupByName)
        {
            var enumValue = new EnumValue(grouping.Key);

            if (grouping.Any(i => i.EnumValue.HasInaccessibleDirective()))
            {
                enumValue.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
            }

            AddFusionEnumValueDirectives(enumValue, [.. grouping]);

            enumType.Values.Add(enumValue);
        }

        return enumType;
    }

    /// <summary>
    /// Produces a single input type definition by unifying multiple input types that share the
    /// <i>same name</i>. Each of these input types may come from different sources, yet must align
    /// into one coherent definition.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Input-Types">
    /// Specification
    /// </seealso>
    private InputObjectTypeDefinition MergeInputTypes(ImmutableArray<TypeInfo> typeGroup)
    {
        var firstType = typeGroup[0].Type;
        var typeName = firstType.Name;
        var description = firstType.Description;
        var mergedFields = new HashSet<InputFieldDefinition>();

        foreach (var typeInfo in typeGroup.Skip(1))
        {
            description ??= typeInfo.Type.Description;
        }

        var inputObjectType = new InputObjectTypeDefinition(typeName) { Description = description };

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            inputObjectType.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionTypeDirectives(inputObjectType, typeGroup);

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((InputObjectTypeDefinition)i.Type).Fields,
                (i, f) => new InputFieldInfo(f, (InputObjectTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name)
            // Intersection: Field definition count matches type definition count.
            .Where(g => g.Count() == typeGroup.Length);

        foreach (var grouping in fieldGroupByName)
        {
            mergedFields.Add(MergeInputFields([.. grouping]));
        }

        foreach (var mergedField in mergedFields)
        {
            inputObjectType.Fields.Add(mergedField);
        }

        return inputObjectType;
    }

    /// <summary>
    /// Merges multiple input field definitions, all sharing the same field name, into a single
    /// composed input field. This ensures the final input type in a composed schema maintains a
    /// consistent type, description, and default value for that field.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Input-Field">
    /// Specification
    /// </seealso>
    private InputFieldDefinition MergeInputFields(ImmutableArray<InputFieldInfo> inputFieldGroup)
    {
        var firstField = inputFieldGroup[0].Field;
        var fieldName = firstField.Name;
        var fieldType = firstField.Type;
        var description = firstField.Description;
        var defaultValue = firstField.DefaultValue;

        foreach (var inputFieldInfo in inputFieldGroup.Skip(1))
        {
            fieldType = MostRestrictiveType(fieldType, inputFieldInfo.Field.Type);
            description ??= inputFieldInfo.Field.Description;
            defaultValue ??= inputFieldInfo.Field.DefaultValue;
        }

        var inputField = new InputFieldDefinition(fieldName)
        {
            DefaultValue = defaultValue,
            Description = description,
            Type = fieldType
        };

        if (inputFieldGroup.Any(i => i.Field.HasInaccessibleDirective()))
        {
            inputField.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionInputFieldDirectives(inputField, inputFieldGroup);

        return inputField;
    }

    /// <summary>
    /// Unifies multiple interface definitions (all sharing the same name) into a single composed
    /// interface type.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Interface-Types">
    /// Specification
    /// </seealso>
    private InterfaceTypeDefinition MergeInterfaceTypes(ImmutableArray<TypeInfo> typeGroup)
    {
        var firstType = typeGroup[0].Type;
        var typeName = firstType.Name;
        var description = firstType.Description;
        var mergedFields = new HashSet<OutputFieldDefinition>();

        foreach (var typeInfo in typeGroup.Skip(1))
        {
            description ??= typeInfo.Type.Description;
        }

        var interfaceType = new InterfaceTypeDefinition(typeName) { Description = description };

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            interfaceType.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionTypeDirectives(interfaceType, typeGroup);

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((InterfaceTypeDefinition)i.Type).Fields,
                (i, f) => new OutputFieldInfo(f, (ComplexTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name);

        foreach (var grouping in fieldGroupByName)
        {
            var mergedField = MergeOutputFields([.. grouping]);

            if (mergedField is not null)
            {
                mergedFields.Add(mergedField);
            }
        }

        foreach (var mergedField in mergedFields)
        {
            interfaceType.Fields.Add(mergedField);
        }

        return interfaceType;
    }

    /// <summary>
    /// Combines multiple object type definitions (all sharing the <i>same name</i>) into a single
    /// composed type. It processes each candidate type, discarding any that are inaccessible or
    /// internal, and then unifies their descriptions and fields.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Object-Types">
    /// Specification
    /// </seealso>
    private ObjectTypeDefinition? MergeObjectTypes(ImmutableArray<TypeInfo> typeGroup)
    {
        // Filter out all types marked with @internal.
        typeGroup = [.. typeGroup.Where(i => !i.Type.HasInternalDirective())];

        if (typeGroup.Length == 0)
        {
            return null;
        }

        var firstType = typeGroup[0].Type;
        var typeName = firstType.Name;
        var description = firstType.Description;
        var mergedFields = new HashSet<OutputFieldDefinition>();

        foreach (var typeInfo in typeGroup.Skip(1))
        {
            description ??= typeInfo.Type.Description;
        }

        var objectType = new ObjectTypeDefinition(typeName) { Description = description };

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            objectType.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionTypeDirectives(objectType, typeGroup);

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((ObjectTypeDefinition)i.Type).Fields,
                (i, f) => new OutputFieldInfo(f, (ComplexTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name);

        foreach (var grouping in fieldGroupByName)
        {
            var mergedField = MergeOutputFields([.. grouping]);

            if (mergedField is not null)
            {
                mergedFields.Add(mergedField);
            }
        }

        foreach (var mergedField in mergedFields)
        {
            objectType.Fields.Add(mergedField);
        }

        return objectType;
    }

    /// <summary>
    /// Used when multiple fields across different object or interface types share the same field
    /// name and must be merged into a single composed field. This algorithm ensures that the final
    /// composed schema has one definitive definition for that field, resolving differences in type,
    /// description, and arguments.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Output-Field">
    /// Specification
    /// </seealso>
    private OutputFieldDefinition? MergeOutputFields(ImmutableArray<OutputFieldInfo> fieldGroup)
    {
        // Filter out all fields marked with @internal.
        fieldGroup = [.. fieldGroup.Where(i => !i.Field.HasInternalDirective())];

        if (fieldGroup.Length == 0)
        {
            return null;
        }

        var firstField = fieldGroup[0].Field;
        var fieldName = firstField.Name;
        var fieldType = firstField.Type;
        var description = firstField.Description;
        var mergedArguments = new HashSet<InputFieldDefinition>();

        foreach (var fieldInfo in fieldGroup.Skip(1))
        {
            fieldType = LeastRestrictiveType(fieldType, fieldInfo.Field.Type);
            description ??= fieldInfo.Field.Description;
        }

        var outputField = new OutputFieldDefinition(fieldName)
        {
            Description = description,
            Type = fieldType
        };

        // [ArgumentName: [{Argument, Field, Type, Schema}, ...], ...].
        var argumentGroupByName = fieldGroup
            .SelectMany(
                i => i.Field.Arguments,
                (i, a) => new FieldArgumentInfo(a, i.Field, i.Type, i.Schema))
            .Where(i => !i.Argument.HasRequireDirective())
            .GroupBy(i => i.Argument.Name)
            // Intersection: Argument definition count matches field definition count.
            .Where(g => g.Count() == fieldGroup.Length);

        foreach (var grouping in argumentGroupByName)
        {
            var mergedArgument = MergeArgumentDefinitions([.. grouping]);

            if (mergedArgument is not null)
            {
                mergedArguments.Add(mergedArgument);
            }
        }

        foreach (var mergedArgument in mergedArguments)
        {
            outputField.Arguments.Add(mergedArgument);
        }

        if (fieldGroup.Any(i => i.Field.HasInaccessibleDirective()))
        {
            outputField.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionFieldDirectives(outputField, fieldGroup);
        AddFusionRequiresDirectives(outputField, fieldGroup);

        return outputField;
    }

    /// <summary>
    /// Merges multiple scalar definitions that share the same name into a single scalar type. It
    /// unifies descriptions so that the final type retains the first available non-<c>null</c>
    /// description.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Scalar-Types">
    /// Specification
    /// </seealso>
    private ScalarTypeDefinition MergeScalarTypes(ImmutableArray<TypeInfo> typeGroup)
    {
        var firstScalar = typeGroup[0].Type;
        var description = firstScalar.Description;

        foreach (var typeInfo in typeGroup.Skip(1))
        {
            description ??= typeInfo.Type.Description;
        }

        var scalarType = new ScalarTypeDefinition(firstScalar.Name)
        {
            Description = description
        };

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            scalarType.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionTypeDirectives(scalarType, typeGroup);

        return scalarType;
    }

    /// <summary>
    /// Aggregates multiple union type definitions that share the <i>same name</i> into one unified
    /// union type. This process excludes possible types marked with <c>@internal</c>.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Union-Types">
    /// Specification
    /// </seealso>
    private UnionTypeDefinition? MergeUnionTypes(
        ImmutableArray<TypeInfo> typeGroup,
        SchemaDefinition mergedSchema)
    {
        var firstUnion = typeGroup[0].Type;
        var name = firstUnion.Name;
        var description = firstUnion.Description;

        foreach (var typeInfo in typeGroup.Skip(1))
        {
            description ??= typeInfo.Type.Description;
        }

        // [UnionMemberName: [{MemberType, UnionType, Schema}, ...], ...].
        var unionMemberGroupByName = typeGroup
            .SelectMany(
                i => ((UnionTypeDefinition)i.Type).Types,
                (i, t) => new UnionMemberInfo(t, (UnionTypeDefinition)i.Type, i.Schema))
            .Where(i => !i.MemberType.HasInternalDirective())
            .GroupBy(i => i.MemberType.Name)
            // Intersection: Member type definition count matches union type definition count.
            .Where(g => g.Count() == typeGroup.Length)
            .ToImmutableArray();

        if (unionMemberGroupByName.Length == 0)
        {
            return null;
        }

        var unionType = new UnionTypeDefinition(name) { Description = description };

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            unionType.Directives.Add(new Directive(new InaccessibleDirectiveDefinition()));
        }

        AddFusionTypeDirectives(unionType, typeGroup);

        foreach (var grouping in unionMemberGroupByName)
        {
            AddFusionUnionMemberDirectives(unionType, [.. grouping]);

            unionType.Types.Add((ObjectTypeDefinition)mergedSchema.Types[grouping.Key]);
        }

        return unionType;
    }

    /// <summary>
    /// Identifies a single type that safely handles all possible runtime values produced by the
    /// sources defining <c>typeA</c> and <c>typeB</c>. If one source can return <c>null</c> while
    /// another cannot, the merged type becomes nullable to avoid runtime exceptions – because a
    /// strictly non-null signature would be violated whenever <c>null</c> appears. Similarly, if
    /// both sources enforce non-null, the result remains non-null.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Least-Restrictive-Type">
    /// Specification
    /// </seealso>
    private static ITypeDefinition LeastRestrictiveType(
        ITypeDefinition typeA,
        ITypeDefinition typeB)
    {
        var isNullable = !(typeA is NonNullTypeDefinition && typeB is NonNullTypeDefinition);

        if (typeA is NonNullTypeDefinition)
        {
            typeA = typeA.NullableType();
        }

        if (typeB is NonNullTypeDefinition)
        {
            typeB = typeB.NullableType();
        }

        if (typeA is ListTypeDefinition)
        {
            Assert(typeB is ListTypeDefinition);

            var innerTypeA = typeA.InnerType();
            var innerTypeB = typeB.InnerType();
            var innerType = LeastRestrictiveType(innerTypeA, innerTypeB);

            return isNullable
                ? new ListTypeDefinition(innerType)
                : new NonNullTypeDefinition(new ListTypeDefinition(innerType));
        }

        Assert(typeA.Equals(typeB, TypeComparison.Structural));

        return isNullable ? typeA : new NonNullTypeDefinition(typeA);
    }

    /// <summary>
    /// Determines a single input type that strictly honors the constraints of both sources. If
    /// either source requires a non-null value, the merged type also becomes non-null so that no
    /// invalid (e.g., <c>null</c>) data can be introduced at runtime. Conversely, if both sources
    /// allow <c>null</c>, the merged type remains nullable. The same principle applies to list
    /// types, where the more restrictive settings (non-null list or non-null elements) is used.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Most-Restrictive-Type">
    /// Specification
    /// </seealso>
    private static ITypeDefinition MostRestrictiveType(
        ITypeDefinition typeA,
        ITypeDefinition typeB)
    {
        var isNullable = typeA is not NonNullTypeDefinition && typeB is not NonNullTypeDefinition;

        if (typeA is NonNullTypeDefinition)
        {
            typeA = typeA.NullableType();
        }

        if (typeB is NonNullTypeDefinition)
        {
            typeB = typeB.NullableType();
        }

        if (typeA is ListTypeDefinition)
        {
            Assert(typeB is ListTypeDefinition);

            var innerTypeA = typeA.InnerType();
            var innerTypeB = typeB.InnerType();
            var innerType = MostRestrictiveType(innerTypeA, innerTypeB);

            return isNullable
                ? new ListTypeDefinition(innerType)
                : new NonNullTypeDefinition(new ListTypeDefinition(innerType));
        }

        Assert(typeA.Equals(typeB, TypeComparison.Structural));

        return isNullable ? typeA : new NonNullTypeDefinition(typeA);
    }

    private static void SetOperationTypes(SchemaDefinition mergedSchema)
    {
        if (mergedSchema.Types.TryGetType(TypeNames.Query, out var queryType))
        {
            mergedSchema.QueryType = (ObjectTypeDefinition?)queryType;
        }

        if (mergedSchema.Types.TryGetType(TypeNames.Mutation, out var mutationType))
        {
            mergedSchema.MutationType = (ObjectTypeDefinition?)mutationType;
        }

        if (mergedSchema.Types.TryGetType(TypeNames.Subscription, out var subscriptionType))
        {
            mergedSchema.SubscriptionType = (ObjectTypeDefinition?)subscriptionType;
        }
    }

    private void AddFusionEnumValueDirectives(
        EnumValue enumValue,
        ImmutableArray<EnumValueInfo> enumValueGroup)
    {
        foreach (var (_, _, sourceSchema) in enumValueGroup)
        {
            var schema = new EnumValueNode(_schemaConstantNames[sourceSchema]);

            enumValue.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionEnumValue],
                    new ArgumentAssignment(ArgumentNames.Schema, schema)));
        }
    }

    private void AddFusionFieldDirectives(
        OutputFieldDefinition field,
        ImmutableArray<OutputFieldInfo> fieldGroup)
    {
        foreach (var (sourceField, _, sourceSchema) in fieldGroup)
        {
            List<ArgumentAssignment> arguments =
                [new(ArgumentNames.Schema, new EnumValueNode(_schemaConstantNames[sourceSchema]))];

            if (!sourceField.Type.Equals(field.Type, TypeComparison.Structural))
            {
                arguments.Add(
                    new ArgumentAssignment(
                        ArgumentNames.SourceType,
                        sourceField.Type.ToTypeNode().Print()));
            }

            if (sourceField.GetProvidesSelectionSet() is { } selectionSet)
            {
                arguments.Add(new ArgumentAssignment(ArgumentNames.Provides, selectionSet));
            }

            if (sourceField.HasExternalDirective())
            {
                arguments.Add(new ArgumentAssignment(ArgumentNames.External, true));
            }

            field.Directives.Add(
                new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionField], arguments));
        }
    }

    private void AddFusionInputFieldDirectives(
        InputFieldDefinition argument,
        ImmutableArray<FieldArgumentInfo> argumentGroup)
    {
        foreach (var (sourceArgument, _, _, sourceSchema) in argumentGroup)
        {
            List<ArgumentAssignment> arguments =
                [new(ArgumentNames.Schema, new EnumValueNode(_schemaConstantNames[sourceSchema]))];

            if (!sourceArgument.Type.Equals(argument.Type, TypeComparison.Structural))
            {
                arguments.Add(
                    new ArgumentAssignment(
                        ArgumentNames.SourceType,
                        sourceArgument.Type.ToTypeNode().Print()));
            }

            argument.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionInputField],
                    arguments));
        }
    }

    private void AddFusionInputFieldDirectives(
        InputFieldDefinition inputField,
        ImmutableArray<InputFieldInfo> inputFieldGroup)
    {
        foreach (var (sourceInputField, _, sourceSchema) in inputFieldGroup)
        {
            List<ArgumentAssignment> arguments =
                [new(ArgumentNames.Schema, new EnumValueNode(_schemaConstantNames[sourceSchema]))];

            if (!sourceInputField.Type.Equals(inputField.Type, TypeComparison.Structural))
            {
                arguments.Add(
                    new ArgumentAssignment(
                        ArgumentNames.SourceType,
                        sourceInputField.Type.ToTypeNode().Print()));
            }

            inputField.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionInputField],
                    arguments));
        }
    }

    private void AddFusionLookupDirectives(
        INamedTypeDefinition type,
        ImmutableArray<LookupFieldInfo> lookupFieldGroup)
    {
        foreach (var (sourceField, sourcePath, sourceSchema) in lookupFieldGroup)
        {
            var schemaArgument = new EnumValueNode(_schemaConstantNames[sourceSchema]);
            var lookupMap = GetFusionLookupMap(sourceField);
            var selectedValues = lookupMap.Select(a => new FieldSelectionMapParser(a).Parse());
            var selectionSets = selectedValues
                .Select(SelectedValueToSelectionSetRewriter.SelectedValueToSelectionSet)
                .ToImmutableArray();
            // FIXME: Merge selection sets. Waiting for selection set merge utility.
            var keyArgument =
                selectionSets[0].ToString(indented: false).AsSpan()[2 .. ^2].ToString();

            var fieldArgument =
                RemoveDirectivesRewriter
                    .Rewrite(sourceField.ToSyntaxNode())!
                    .ToString(indented: false);

            var mapArgument = new ListValueNode(lookupMap.ConvertAll(a => new StringValueNode(a)));

            IValueNode pathArgument = sourcePath is null
                ? NullValueNode.Default
                : new StringValueNode(sourcePath);

            type.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionLookup],
                    new ArgumentAssignment(ArgumentNames.Schema, schemaArgument),
                    new ArgumentAssignment(ArgumentNames.Key, keyArgument),
                    new ArgumentAssignment(ArgumentNames.Field, fieldArgument),
                    new ArgumentAssignment(ArgumentNames.Map, mapArgument),
                    new ArgumentAssignment(ArgumentNames.Path, pathArgument)));
        }
    }

    // productById(id: ID!) -> ["id"].
    // productByIdAndCategoryId(id: ID!, categoryId: Int) -> ["id", "categoryId"].
    // personByAddressId(id: ID! @is(field: "address.id")) -> ["address.id"].
    private static List<string> GetFusionLookupMap(OutputFieldDefinition field)
    {
        var items = new List<string>();

        foreach (var argument in field.Arguments)
        {
            var @is = argument.GetIsFieldSelectionMap();

            items.Add(@is ?? argument.Name);
        }

        return items;
    }

    private void AddFusionRequiresDirectives(
        OutputFieldDefinition field,
        ImmutableArray<OutputFieldInfo> fieldGroup)
    {
        foreach (var (sourceField, _, sourceSchema) in fieldGroup)
        {
            List<string?> map = [];

            foreach (var argument in sourceField.Arguments)
            {
                var requireDirective = argument.Directives.FirstOrDefault(DirectiveNames.Require);

                if (requireDirective?.Arguments[ArgumentNames.Field] is StringValueNode fieldArg)
                {
                    map.Add(fieldArg.Value);
                }
                else
                {
                    map.Add(null);
                }
            }

            if (map.Any(v => v is not null))
            {
                var schemaArgument = new EnumValueNode(_schemaConstantNames[sourceSchema]);

                var fieldArgument =
                    RemoveDirectivesRewriter
                        .Rewrite(sourceField.ToSyntaxNode())!
                        .ToString(indented: false);

                var mapArgument = new ListValueNode(
                    map.ConvertAll<IValueNode>(
                        v => v is null ? NullValueNode.Default : new StringValueNode(v)));

                field.Directives.Add(
                    new Directive(
                        _fusionDirectiveDefinitions[DirectiveNames.FusionRequires],
                        new ArgumentAssignment(ArgumentNames.Schema, schemaArgument),
                        new ArgumentAssignment(ArgumentNames.Field, fieldArgument),
                        new ArgumentAssignment(ArgumentNames.Map, mapArgument)));
            }
        }
    }

    private void AddFusionTypeDirectives(
        IDirectivesProvider type,
        ImmutableArray<TypeInfo> typeGroup)
    {
        foreach (var (_, sourceSchema) in typeGroup)
        {
            var schema = new EnumValueNode(_schemaConstantNames[sourceSchema]);

            type.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionType],
                    new ArgumentAssignment(ArgumentNames.Schema, schema)));
        }
    }

    private void AddFusionUnionMemberDirectives(
        UnionTypeDefinition unionType,
        ImmutableArray<UnionMemberInfo> unionMemberGroup)
    {
        foreach (var (sourceMemberType, _, sourceSchema) in unionMemberGroup)
        {
            var schema = new EnumValueNode(_schemaConstantNames[sourceSchema]);

            unionType.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionUnionMember],
                    new ArgumentAssignment(ArgumentNames.Schema, schema),
                    new ArgumentAssignment(ArgumentNames.Member, sourceMemberType.Name)));
        }
    }

    private FrozenDictionary<string, INamedTypeDefinition> CreateFusionTypeDefinitions()
    {
        return new Dictionary<string, INamedTypeDefinition>
        {
            // Scalar type definitions.
            {
                TypeNames.FusionFieldDefinition,
                ScalarTypeDefinition.Create(TypeNames.FusionFieldDefinition)
            },
            {
                TypeNames.FusionFieldSelectionMap,
                ScalarTypeDefinition.Create(TypeNames.FusionFieldSelectionMap)
            },
            {
                TypeNames.FusionFieldSelectionPath,
                ScalarTypeDefinition.Create(TypeNames.FusionFieldSelectionPath)
            },
            {
                TypeNames.FusionFieldSelectionSet,
                ScalarTypeDefinition.Create(TypeNames.FusionFieldSelectionSet)
            },
            // Enum type definitions.
            {
                TypeNames.FusionSchema,
                new FusionSchemaEnumTypeDefinition([.. _schemas.Select(s => s.Name)])
            }
        }.ToFrozenDictionary();
    }

    private FrozenDictionary<string, DirectiveDefinition> CreateFusionDirectiveDefinitions()
    {
        var schemaEnumType = (EnumTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionSchema];
        var fieldDefinitionType =
            (ScalarTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionFieldDefinition];
        var fieldSelectionMapType =
            (ScalarTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionFieldSelectionMap];
        var fieldSelectionPathType =
            (ScalarTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionFieldSelectionPath];
        var fieldSelectionSetType =
            (ScalarTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionFieldSelectionSet];
        var stringType = BuiltIns.String.Create();
        var booleanType = BuiltIns.Boolean.Create();

        return new Dictionary<string, DirectiveDefinition>()
        {
            {
                DirectiveNames.FusionEnumValue,
                new FusionEnumValueDirectiveDefinition(schemaEnumType)
            },
            {
                DirectiveNames.FusionField,
                new FusionFieldDirectiveDefinition(
                    schemaEnumType,
                    stringType,
                    fieldSelectionSetType,
                    booleanType)
            },
            {
                DirectiveNames.FusionImplements,
                new FusionImplementsDirectiveDefinition(schemaEnumType, stringType)
            },
            {
                DirectiveNames.FusionInputField,
                new FusionInputFieldDirectiveDefinition(schemaEnumType, stringType)
            },
            {
                DirectiveNames.FusionLookup,
                new FusionLookupDirectiveDefinition(
                    schemaEnumType,
                    fieldSelectionSetType,
                    fieldDefinitionType,
                    fieldSelectionMapType,
                    fieldSelectionPathType)
            },
            {
                DirectiveNames.FusionRequires,
                new FusionRequiresDirectiveDefinition(
                    schemaEnumType,
                    fieldDefinitionType,
                    fieldSelectionMapType)
            },
            {
                DirectiveNames.FusionType,
                new FusionTypeDirectiveDefinition(schemaEnumType)
            },
            {
                DirectiveNames.FusionUnionMember,
                new FusionUnionMemberDirectiveDefinition(schemaEnumType, stringType)
            }
        }.ToFrozenDictionary();
    }

    private void AddFusionDefinitions(SchemaDefinition mergedSchema)
    {
        foreach (var (_, definition) in _fusionTypeDefinitions)
        {
            mergedSchema.Types.Add(definition);
        }

        foreach (var (_, definition) in _fusionDirectiveDefinitions)
        {
            mergedSchema.DirectiveDefinitions.Add(definition);
        }
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException();
        }
    }
}
