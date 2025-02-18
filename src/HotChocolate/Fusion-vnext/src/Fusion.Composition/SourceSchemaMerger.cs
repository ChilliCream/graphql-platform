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
            TypeKind.Enum => MergeEnumTypes(typeGroup, mergedSchema),
            TypeKind.InputObject => MergeInputTypes(typeGroup, mergedSchema),
            TypeKind.Interface => MergeInterfaceTypes(typeGroup, mergedSchema),
            TypeKind.Object => MergeObjectTypes(typeGroup, mergedSchema),
            TypeKind.Scalar => MergeScalarTypes(typeGroup, mergedSchema),
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
        ImmutableArray<FieldArgumentInfo> argumentGroup,
        SchemaDefinition mergedSchema)
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

        mergedArgument.Type = mergedArgument.Type.ReplaceNameType(
            _ => GetOrCreateType(mergedSchema, mergedArgument.Type));

        AddFusionInputFieldDirectives(mergedArgument, argumentGroup);

        if (argumentGroup.Any(i => i.Argument.HasInaccessibleDirective()))
        {
            mergedArgument.Directives.Add(
                new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

        return mergedArgument;
    }

    /// <summary>
    /// Consolidates multiple enum definitions (all sharing the <i>same name</i>) into one final
    /// enum type.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Enum-Types">
    /// Specification
    /// </seealso>
    private EnumTypeDefinition MergeEnumTypes(
        ImmutableArray<TypeInfo> typeGroup,
        SchemaDefinition mergedSchema)
    {
        var firstEnum = typeGroup[0].Type;
        var typeName = firstEnum.Name;
        var description = firstEnum.Description;
        var enumType = GetOrCreateType<EnumTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        enumType.Description = description;

        AddFusionTypeDirectives(enumType, typeGroup);

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            enumType.Directives.Add(new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

        // [EnumValueName: [{EnumValue, EnumType, Schema}, ...], ...].
        var enumValueGroupByName = typeGroup
            .SelectMany(
                i => ((EnumTypeDefinition)i.Type).Values,
                (i, v) => new EnumValueInfo(v, (EnumTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.EnumValue.Name);

        foreach (var enumValueGroup in enumValueGroupByName)
        {
            enumType.Values.Add(MergeEnumValues([.. enumValueGroup]));
        }

        return enumType;
    }

    /// <summary>
    /// Merges multiple enum value definitions, all sharing the same name, into a single composed
    /// enum value. This ensures the final enum value in a composed schema maintains a consistent
    /// description.
    /// </summary>
    private EnumValue MergeEnumValues(ImmutableArray<EnumValueInfo> enumValueGroup)
    {
        var firstValue = enumValueGroup[0].EnumValue;
        var valueName = firstValue.Name;
        var description = firstValue.Description;

        for (var i = 1; i < enumValueGroup.Length; i++)
        {
            description ??= enumValueGroup[i].EnumValue.Description;
        }

        var enumValue = new EnumValue(valueName) { Description = description };

        AddFusionEnumValueDirectives(enumValue, enumValueGroup);

        if (enumValueGroup.Any(i => i.EnumValue.HasInaccessibleDirective()))
        {
            enumValue.Directives.Add(new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

        return enumValue;
    }

    /// <summary>
    /// Produces a single input type definition by unifying multiple input types that share the
    /// <i>same name</i>. Each of these input types may come from different sources, yet must align
    /// into one coherent definition.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Input-Types">
    /// Specification
    /// </seealso>
    private InputObjectTypeDefinition MergeInputTypes(
        ImmutableArray<TypeInfo> typeGroup,
        SchemaDefinition mergedSchema)
    {
        var firstType = typeGroup[0].Type;
        var typeName = firstType.Name;
        var description = firstType.Description;
        var inputObjectType = GetOrCreateType<InputObjectTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        inputObjectType.Description = description;

        AddFusionTypeDirectives(inputObjectType, typeGroup);

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            inputObjectType.Directives.Add(
                new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((InputObjectTypeDefinition)i.Type).Fields,
                (i, f) => new InputFieldInfo(f, (InputObjectTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name)
            // Intersection: Field definition count matches type definition count.
            .Where(g => g.Count() == typeGroup.Length);

        foreach (var fieldGroup in fieldGroupByName)
        {
            inputObjectType.Fields.Add(MergeInputFields([.. fieldGroup], mergedSchema));
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
    private InputFieldDefinition MergeInputFields(
        ImmutableArray<InputFieldInfo> inputFieldGroup,
        SchemaDefinition mergedSchema)
    {
        var firstField = inputFieldGroup[0].Field;
        var fieldName = firstField.Name;
        var fieldType = firstField.Type;
        var description = firstField.Description;
        var defaultValue = firstField.DefaultValue;

        for (var i = 1; i < inputFieldGroup.Length; i++)
        {
            var inputFieldInfo = inputFieldGroup[i];
            fieldType = MostRestrictiveType(fieldType, inputFieldInfo.Field.Type);
            description ??= inputFieldInfo.Field.Description;
            defaultValue ??= inputFieldInfo.Field.DefaultValue;
        }

        var inputField = new InputFieldDefinition(fieldName)
        {
            DefaultValue = defaultValue,
            Description = description,
            Type = fieldType.ReplaceNameType(_ => GetOrCreateType(mergedSchema, fieldType))
        };

        AddFusionInputFieldDirectives(inputField, inputFieldGroup);

        if (inputFieldGroup.Any(i => i.Field.HasInaccessibleDirective()))
        {
            inputField.Directives.Add(new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

        return inputField;
    }

    /// <summary>
    /// Unifies multiple interface definitions (all sharing the same name) into a single composed
    /// interface type.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Interface-Types">
    /// Specification
    /// </seealso>
    private InterfaceTypeDefinition MergeInterfaceTypes(
        ImmutableArray<TypeInfo> typeGroup,
        SchemaDefinition mergedSchema)
    {
        var firstType = typeGroup[0].Type;
        var typeName = firstType.Name;
        var description = firstType.Description;
        var interfaceType = GetOrCreateType<InterfaceTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        interfaceType.Description = description;

        // [InterfaceName: [{InterfaceType, Schema}, ...], ...].
        var interfaceGroupByName = typeGroup
            .SelectMany(
                i => ((InterfaceTypeDefinition)i.Type).Implements,
                (i, it) => new InterfaceInfo(it, i.Schema))
            .GroupBy(i => i.InterfaceType.Name)
            .Where(g => !g.Any(i => i.InterfaceType.HasInaccessibleDirective()))
            .ToArray();

        foreach (var (interfaceName, _) in interfaceGroupByName)
        {
            interfaceType.Implements.Add(
                GetOrCreateType<InterfaceTypeDefinition>(mergedSchema, interfaceName));
        }

        AddFusionTypeDirectives(interfaceType, typeGroup);
        AddFusionImplementsDirectives(interfaceType, [.. interfaceGroupByName.SelectMany(g => g)]);

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            interfaceType.Directives.Add(
                new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((InterfaceTypeDefinition)i.Type).Fields,
                (i, f) => new OutputFieldInfo(f, (ComplexTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name);

        foreach (var fieldGroup in fieldGroupByName)
        {
            var mergedField = MergeOutputFields([.. fieldGroup], mergedSchema);

            if (mergedField is not null)
            {
                interfaceType.Fields.Add(mergedField);
            }
        }

        return interfaceType;
    }

    /// <summary>
    /// Combines multiple object type definitions (all sharing the <i>same name</i>) into a single
    /// composed type. It processes each candidate type, discarding any that are internal, and then
    /// unifies their descriptions and fields.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Object-Types">
    /// Specification
    /// </seealso>
    private ObjectTypeDefinition? MergeObjectTypes(
        ImmutableArray<TypeInfo> typeGroup,
        SchemaDefinition mergedSchema)
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
        var objectType = GetOrCreateType<ObjectTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        objectType.Description = description;

        // [InterfaceName: [{InterfaceType, Schema}, ...], ...].
        var interfaceGroupByName = typeGroup
            .SelectMany(
                i => ((ObjectTypeDefinition)i.Type).Implements,
                (i, it) => new InterfaceInfo(it, i.Schema))
            .GroupBy(i => i.InterfaceType.Name)
            .Where(g => !g.Any(i => i.InterfaceType.HasInaccessibleDirective()))
            .ToArray();

        foreach (var (interfaceName, _) in interfaceGroupByName)
        {
            objectType.Implements.Add(
                GetOrCreateType<InterfaceTypeDefinition>(mergedSchema, interfaceName));
        }

        AddFusionTypeDirectives(objectType, typeGroup);
        AddFusionImplementsDirectives(objectType, [.. interfaceGroupByName.SelectMany(g => g)]);

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            objectType.Directives.Add(new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((ObjectTypeDefinition)i.Type).Fields,
                (i, f) => new OutputFieldInfo(f, (ComplexTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name);

        foreach (var fieldGroup in fieldGroupByName)
        {
            var mergedField = MergeOutputFields([.. fieldGroup], mergedSchema);

            if (mergedField is not null)
            {
                objectType.Fields.Add(mergedField);
            }
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
    private OutputFieldDefinition? MergeOutputFields(
        ImmutableArray<OutputFieldInfo> fieldGroup,
        SchemaDefinition mergedSchema)
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

        for (var i = 1; i < fieldGroup.Length; i++)
        {
            var fieldInfo = fieldGroup[i];
            fieldType = LeastRestrictiveType(fieldType, fieldInfo.Field.Type);
            description ??= fieldInfo.Field.Description;
        }

        var outputField = new OutputFieldDefinition(fieldName)
        {
            Description = description,
            Type = fieldType.ReplaceNameType(_ => GetOrCreateType(mergedSchema, fieldType))
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

        foreach (var argumentGroup in argumentGroupByName)
        {
            var mergedArgument = MergeArgumentDefinitions([.. argumentGroup], mergedSchema);

            if (mergedArgument is not null)
            {
                outputField.Arguments.Add(mergedArgument);
            }
        }

        AddFusionFieldDirectives(outputField, fieldGroup);
        AddFusionRequiresDirectives(outputField, fieldGroup);

        if (fieldGroup.Any(i => i.Field.HasInaccessibleDirective()))
        {
            outputField.Directives.Add(new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

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
    private ScalarTypeDefinition MergeScalarTypes(
        ImmutableArray<TypeInfo> typeGroup,
        SchemaDefinition mergedSchema)
    {
        var firstScalar = typeGroup[0].Type;
        var typeName = firstScalar.Name;
        var description = firstScalar.Description;
        var scalarType = GetOrCreateType<ScalarTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        scalarType.Description = description;

        AddFusionTypeDirectives(scalarType, typeGroup);

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            scalarType.Directives.Add(new Directive(new FusionInaccessibleDirectiveDefinition()));
        }

        return scalarType;
    }

    /// <summary>
    /// Aggregates multiple union type definitions that share the <i>same name</i> into one unified
    /// union type. This process excludes possible types marked with <c>@internal</c>.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Union-Types">
    /// Specification
    /// </seealso>
    private UnionTypeDefinition MergeUnionTypes(
        ImmutableArray<TypeInfo> typeGroup,
        SchemaDefinition mergedSchema)
    {
        var firstUnion = typeGroup[0].Type;
        var name = firstUnion.Name;
        var description = firstUnion.Description;
        var unionType = GetOrCreateType<UnionTypeDefinition>(mergedSchema, name);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        unionType.Description = description;

        AddFusionTypeDirectives(unionType, typeGroup);

        // [UnionMemberName: [{MemberType, UnionType, Schema}, ...], ...].
        var unionMemberGroupByName = typeGroup
            .SelectMany(
                i => ((UnionTypeDefinition)i.Type).Types,
                (i, t) => new UnionMemberInfo(t, (UnionTypeDefinition)i.Type, i.Schema))
            .Where(i => !i.MemberType.HasInternalDirective())
            .GroupBy(i => i.MemberType.Name)
            // Intersection: Member type definition count matches union type definition count.
            .Where(g => g.Count() == typeGroup.Length);

        foreach (var (memberName, memberGroup) in unionMemberGroupByName)
        {
            AddFusionUnionMemberDirectives(unionType, [.. memberGroup]);

            unionType.Types.Add(GetOrCreateType<ObjectTypeDefinition>(mergedSchema, memberName));
        }

        if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
        {
            unionType.Directives.Add(new Directive(new FusionInaccessibleDirectiveDefinition()));
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

        if (mergedSchema.Types.TryGetType(TypeNames.Mutation, out var mutationType)
            && mutationType is ObjectTypeDefinition mutationObjectType)
        {
            if (mutationObjectType.Fields.Count == 0)
            {
                mergedSchema.Types.Remove(mutationObjectType);
            }
            else
            {
                mergedSchema.MutationType = mutationObjectType;
            }
        }

        if (mergedSchema.Types.TryGetType(TypeNames.Subscription, out var subscriptionType)
            && subscriptionType is ObjectTypeDefinition subscriptionObjectType)
        {
            if (subscriptionObjectType.Fields.Count == 0)
            {
                mergedSchema.Types.Remove(subscriptionObjectType);
            }
            else
            {
                mergedSchema.SubscriptionType = subscriptionObjectType;
            }
        }
    }

    private static T GetOrCreateType<T>(SchemaDefinition mergedSchema, string typeName)
        where T : class, INamedTypeSystemMemberDefinition<T>
    {
        if (mergedSchema.Types.TryGetType(typeName, out var existingType))
        {
            return (T)existingType;
        }

        var newType = T.Create(typeName);

        mergedSchema.Types.Add((INamedTypeDefinition)newType);

        return newType;
    }

    private static INamedTypeDefinition GetOrCreateType(
        SchemaDefinition mergedSchema,
        ITypeDefinition sourceType)
    {
        return sourceType switch
        {
            EnumTypeDefinition e
                => GetOrCreateType<EnumTypeDefinition>(mergedSchema, e.Name),
            InputObjectTypeDefinition i
                => GetOrCreateType<InputObjectTypeDefinition>(mergedSchema, i.Name),
            InterfaceTypeDefinition i
                => GetOrCreateType<InterfaceTypeDefinition>(mergedSchema, i.Name),
            ListTypeDefinition l
                => GetOrCreateType(mergedSchema, l.ElementType),
            MissingTypeDefinition m
                => new MissingTypeDefinition(m.Name),
            NonNullTypeDefinition n
                => GetOrCreateType(mergedSchema, n.NullableType),
            ObjectTypeDefinition o
                => GetOrCreateType<ObjectTypeDefinition>(mergedSchema, o.Name),
            ScalarTypeDefinition s
                => GetOrCreateType<ScalarTypeDefinition>(mergedSchema, s.Name),
            UnionTypeDefinition u
                => GetOrCreateType<UnionTypeDefinition>(mergedSchema, u.Name),
            _
                => throw new ArgumentOutOfRangeException(nameof(sourceType))
        };
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

    private void AddFusionImplementsDirectives(
        ComplexTypeDefinition complexType,
        ImmutableArray<InterfaceInfo> interfaceGroup)
    {
        foreach (var (sourceInterface, sourceSchema) in interfaceGroup)
        {
            complexType.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionImplements],
                    new ArgumentAssignment(
                        ArgumentNames.Schema,
                        new EnumValueNode(_schemaConstantNames[sourceSchema])),
                    new ArgumentAssignment(ArgumentNames.Interface, sourceInterface.Name)));
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
