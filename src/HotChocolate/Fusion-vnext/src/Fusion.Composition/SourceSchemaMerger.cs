using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.DirectiveMergers;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.SchemaVisitors;
using HotChocolate.Fusion.SyntaxRewriters;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.StringUtilities;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;
using FieldNames = HotChocolate.Fusion.WellKnownFieldNames;
using TypeNames = HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaMerger
{
    private static readonly FusionFieldDefinitionSyntaxRewriter s_fieldDefinitionRewriter = new();
    private readonly ImmutableSortedSet<MutableSchemaDefinition> _schemas;
    private readonly FrozenDictionary<string, string> _schemaConstantNames;
    private readonly SourceSchemaMergerOptions _options;
    private readonly FrozenDictionary<string, ITypeDefinition> _fusionTypeDefinitions;
    private readonly FrozenDictionary<string, MutableDirectiveDefinition>
        _fusionDirectiveDefinitions;
    private readonly Dictionary<string, ValueSelectionToSelectionSetRewriter>
        _selectedValueToSelectionSetRewriters = [];
    private readonly Dictionary<string, MergeSelectionSetRewriter> _mergeSelectionSetRewriters = [];
    private readonly FrozenDictionary<string, IDirectiveMerger> _directiveMergers;
    private readonly List<Action> _applyDirectiveActions = [];

    public SourceSchemaMerger(
        ImmutableSortedSet<MutableSchemaDefinition> schemas,
        SourceSchemaMergerOptions? options = null)
    {
        _schemas = schemas;
        _schemaConstantNames = schemas.ToFrozenDictionary(s => s.Name, s => ToConstantCase(s.Name));
        _options = options ?? new SourceSchemaMergerOptions();
        _fusionTypeDefinitions = CreateFusionTypeDefinitions();
        _fusionDirectiveDefinitions = CreateFusionDirectiveDefinitions();
        _directiveMergers =
            new Dictionary<string, IDirectiveMerger>
            {
                {
                    DirectiveNames.CacheControl,
                    new CacheControlDirectiveMerger(_options.CacheControlMergeBehavior)
                },
                {
                    DirectiveNames.McpToolAnnotations,
                    new McpToolAnnotationsDirectiveMerger(DirectiveMergeBehavior.Include)
                },
                {
                    DirectiveNames.OneOf,
                    new OneOfDirectiveMerger(DirectiveMergeBehavior.Include)
                },
                {
                    DirectiveNames.SerializeAs,
                    new SerializeAsDirectiveMerger(DirectiveMergeBehavior.Include)
                },
                {
                    DirectiveNames.Tag,
                    new TagDirectiveMerger(_options.TagMergeBehavior)
                }
            }.ToFrozenDictionary();
    }

    public CompositionResult<MutableSchemaDefinition> Merge()
    {
        var mergedSchema = new MutableSchemaDefinition();

        MergeTypes(mergedSchema);
        MergeDirectiveDefinitions(mergedSchema);
        ApplyDirectives();
        SetOperationTypes(mergedSchema);
        AddFusionLookupDirectives(mergedSchema);
        AddNodeField(mergedSchema);

        // Merge directives.
        _directiveMergers[DirectiveNames.Tag].MergeDirectives(mergedSchema, [.. _schemas], mergedSchema);

        // Remove unreferenced definitions.
        if (_options.RemoveUnreferencedDefinitions)
        {
            mergedSchema.RemoveUnreferencedDefinitions(GetPreservedInputTypeNames());
        }

        // Add Fusion definitions.
        if (_options.AddFusionDefinitions)
        {
            AddFusionDefinitions(mergedSchema);
        }

        return mergedSchema;
    }

    private void MergeTypes(MutableSchemaDefinition mergedSchema)
    {
        // [TypeName: [{Type, Schema}, ...], ...].
        var typeGroupByName = _schemas
            .SelectMany(s => s.Types, (schema, type) => new TypeInfo(type, schema))
            .GroupBy(i => i.Type.Name);

        foreach (var (_, typeGroup) in typeGroupByName)
        {
            var typeGroupArr = typeGroup.ToImmutableArray();
            var kind = typeGroupArr[0].Type.Kind;

            Assert(typeGroupArr.All(i => i.Type.Kind == kind));

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            ITypeDefinition? mergedType = kind switch
            {
                TypeKind.Enum => MergeEnumTypes(typeGroupArr, mergedSchema),
                TypeKind.InputObject => MergeInputTypes(typeGroupArr, mergedSchema),
                TypeKind.Interface => MergeInterfaceTypes(typeGroupArr, mergedSchema),
                TypeKind.Object => MergeObjectTypes(typeGroupArr, mergedSchema),
                TypeKind.Scalar => MergeScalarTypes(typeGroupArr, mergedSchema),
                TypeKind.Union => MergeUnionTypes(typeGroupArr, mergedSchema),
                _ => throw new InvalidOperationException()
            };

            if (mergedType is not null)
            {
                mergedSchema.Types.Add(mergedType);
            }
        }
    }

    private void MergeDirectiveDefinitions(MutableSchemaDefinition mergedSchema)
    {
        // [DirectiveName: [{DirectiveDefinition, Schema}, ...], ...].
        var directiveDefinitionGroupByName = _schemas
            .SelectMany(
                s => s.DirectiveDefinitions.AsEnumerable(),
                (schema, directiveDefinition) => new DirectiveDefinitionInfo(directiveDefinition, schema))
            .GroupBy(i => i.DirectiveDefinition.Name);

        foreach (var grouping in directiveDefinitionGroupByName)
        {
            if (!_directiveMergers.TryGetValue(grouping.Key, out var directiveMerger)
                || directiveMerger.MergeBehavior is DirectiveMergeBehavior.Ignore)
            {
                continue;
            }

            var canonicalDirectiveDefinition = directiveMerger.GetCanonicalDirectiveDefinition(mergedSchema);
            var canonicalDirectiveNode = canonicalDirectiveDefinition.ToSyntaxNode();

            // Ensure that all directive definitions match the canonical definition.
            if (!grouping.All(
                d => d.DirectiveDefinition
                    .ToSyntaxNode()
                    .Equals(canonicalDirectiveNode, SyntaxComparison.SyntaxIgnoreDescriptions)))
            {
                // Skip merging if there is a mismatch.
                continue;
            }

            directiveMerger.MergeDirectiveDefinition(canonicalDirectiveDefinition, mergedSchema);
        }
    }

    private void ApplyDirectives()
    {
        foreach (var action in _applyDirectiveActions)
        {
            action.Invoke();
        }
    }

    private void AddFusionLookupDirectives(MutableSchemaDefinition mergedSchema)
    {
        foreach (var schema in _schemas)
        {
            var discoverLookups = new DiscoverLookupsSchemaVisitor(schema);

            // [TypeName: [{LookupField, Path, Schema}, ...], ...].
            var lookupFieldGroupByTypeName = discoverLookups.Discover();

            foreach (var (typeName, lookupFieldGroup) in lookupFieldGroupByTypeName)
            {
                if (!mergedSchema.Types.TryGetType<IMutableTypeDefinition>(
                    typeName,
                    out var mergedType))
                {
                    continue;
                }

                foreach (var (sourceField, sourcePath, sourceSchema) in lookupFieldGroup)
                {
                    var schemaArgument = new EnumValueNode(_schemaConstantNames[sourceSchema.Name]);

                    var fieldArgument =
                        s_fieldDefinitionRewriter
                            .Rewrite(sourceField.ToSyntaxNode())!
                            .ToString(indented: false);

                    IValueNode pathArgument = sourcePath is null
                        ? NullValueNode.Default
                        : new StringValueNode(sourcePath);

                    var @internal = sourceField.IsInternal;

                    foreach (var valueSelectionGroup in sourceField.GetValueSelectionGroups())
                    {
                        var keyArgument = sourceField.GetKeyFields(valueSelectionGroup, sourceSchema);

                        var mapArgument =
                            new ListValueNode(
                                valueSelectionGroup.ConvertAll(
                                    a => new StringValueNode(a.ToString(indented: false))));

                        mergedType.Directives.Add(
                            new Directive(
                                _fusionDirectiveDefinitions[DirectiveNames.FusionLookup],
                                new ArgumentAssignment(ArgumentNames.Schema, schemaArgument),
                                new ArgumentAssignment(ArgumentNames.Key, keyArgument),
                                new ArgumentAssignment(ArgumentNames.Field, fieldArgument),
                                new ArgumentAssignment(ArgumentNames.Map, mapArgument),
                                new ArgumentAssignment(ArgumentNames.Path, pathArgument),
                                new ArgumentAssignment(ArgumentNames.Internal, @internal)));
                    }
                }
            }
        }
    }

    private void AddNodeField(MutableSchemaDefinition mergedSchema)
    {
        if (mergedSchema.Types.TryGetType<IInterfaceTypeDefinition>(TypeNames.Node, out var nodeType)
            && mergedSchema.QueryType is { } queryType)
        {
            if (queryType.Fields.TryGetField(FieldNames.Node, out var nodeField)
                && nodeField.Type == nodeType)
            {
                queryType.Fields.Remove(nodeField);
            }

            // Until gateway support is implemented, we never expose the nodes field in the merged schema.
            if (queryType.Fields.TryGetField(FieldNames.Nodes, out var nodesField)
                && nodesField.Type.NamedType() == nodeType)
            {
                queryType.Fields.Remove(nodesField);
            }

            if (_options.EnableGlobalObjectIdentification
                && mergedSchema.Types.TryGetType<IScalarTypeDefinition>(TypeNames.ID, out var idType))
            {
                var canonicalNodeField = new MutableOutputFieldDefinition(FieldNames.Node, nodeType);
                canonicalNodeField.Arguments.Add(
                    new MutableInputFieldDefinition(ArgumentNames.Id, new NonNullType(idType)));

                queryType.Fields.Add(canonicalNodeField);
            }
        }
    }

    /// <summary>
    /// Returns a list of input type names for types that must be preserved in the merged schema
    /// even if they are not directly referenced.
    /// </summary>
    private HashSet<string> GetPreservedInputTypeNames()
    {
        var preservedInputTypeNames = new HashSet<string>();

        foreach (var schema in _schemas)
        {
            foreach (var type in schema.Types.OfType<IObjectTypeDefinition>())
            {
                foreach (var field in type.Fields)
                {
                    foreach (var argument in field.Arguments)
                    {
                        var argumentInnerType = argument.Type.InnerType();

                        if (argumentInnerType is IInputObjectTypeDefinition inputObjectType
                            && (argument.HasRequireDirective || field.IsLookup))
                        {
                            preservedInputTypeNames.Add(inputObjectType.Name);
                        }
                    }
                }
            }
        }

        return preservedInputTypeNames;
    }

    /// <summary>
    /// Takes two arguments with the same name but possibly differing in type, description, or
    /// default value, and returns a single, unified argument definition.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Argument">
    /// Specification
    /// </seealso>
    private static MutableInputFieldDefinition MergeArguments(
        MutableInputFieldDefinition argumentA,
        MutableInputFieldDefinition argumentB)
    {
        var typeA = argumentA.Type;
        var typeB = argumentB.Type;
        var type = MostRestrictiveType(typeA, typeB);
        var description = argumentA.Description ?? argumentB.Description;
        var defaultValue = argumentA.DefaultValue ?? argumentB.DefaultValue;
        var isDeprecated = argumentA.IsDeprecated || argumentB.IsDeprecated;
        var deprecationReason = isDeprecated
            ? argumentA.DeprecationReason ?? argumentB.DeprecationReason
            : null;

        return new MutableInputFieldDefinition(argumentA.Name, type.ExpectInputType())
        {
            DefaultValue = defaultValue,
            Description = description,
            IsDeprecated = isDeprecated,
            DeprecationReason = deprecationReason
        };
    }

    /// <summary>
    /// Merges multiple arguments that share the same name across different field definitions into a
    /// single composed argument definition.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Argument-Definitions">
    /// Specification
    /// </seealso>
    private MutableInputFieldDefinition? MergeArgumentDefinitions(
        ImmutableArray<FieldArgumentInfo> argumentGroup,
        MutableSchemaDefinition mergedSchema)
    {
        Assert(!argumentGroup.Any(i => i.Argument.HasRequireDirective));

        var mergedArgument = argumentGroup.Select(i => i.Argument).FirstOrDefault();

        if (mergedArgument is null)
        {
            return null;
        }

        foreach (var argumentInfo in argumentGroup)
        {
            mergedArgument = MergeArguments(mergedArgument, argumentInfo.Argument);
        }

        mergedArgument.Type = mergedArgument.Type
            .ReplaceNamedType(_ => GetOrCreateType(mergedSchema, mergedArgument.Type))
            .ExpectInputType();

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = argumentGroup.Select(g => g.Argument).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.Tag].MergeDirectives(mergedArgument, memberDefinitions, mergedSchema);

                AddFusionInputFieldDirectives(mergedArgument, argumentGroup);

                if (argumentGroup.Any(i => i.Argument.HasInaccessibleDirective()))
                {
                    mergedArgument.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

        return mergedArgument;
    }

    /// <summary>
    /// Consolidates multiple enum definitions (all sharing the <i>same name</i>) into one final
    /// enum type.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Enum-Types">
    /// Specification
    /// </seealso>
    private MutableEnumTypeDefinition MergeEnumTypes(
        ImmutableArray<TypeInfo> typeGroup,
        MutableSchemaDefinition mergedSchema)
    {
        var firstEnum = typeGroup[0].Type;
        var typeName = firstEnum.Name;
        var description = firstEnum.Description;
        var enumType = GetOrCreateType<MutableEnumTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        enumType.Description = description;

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = typeGroup.Select(g => g.Type).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.Tag].MergeDirectives(enumType, memberDefinitions, mergedSchema);

                AddFusionTypeDirectives(enumType, typeGroup);

                if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
                {
                    enumType.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

        // [EnumValueName: [{EnumValue, EnumType, Schema}, ...], ...].
        var enumValueGroupByName = typeGroup
            .SelectMany(
                i => ((MutableEnumTypeDefinition)i.Type).Values.AsEnumerable(),
                (i, v) => new EnumValueInfo(v, (MutableEnumTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.EnumValue.Name);

        foreach (var enumValueGroup in enumValueGroupByName)
        {
            enumType.Values.Add(MergeEnumValues([.. enumValueGroup], mergedSchema));
        }

        return enumType;
    }

    /// <summary>
    /// Merges multiple enum value definitions, all sharing the same name, into a single composed
    /// enum value. This ensures the final enum value in a composed schema maintains a consistent
    /// description.
    /// </summary>
    private MutableEnumValue MergeEnumValues(
        ImmutableArray<EnumValueInfo> enumValueGroup,
        MutableSchemaDefinition mergedSchema)
    {
        var firstValue = enumValueGroup[0].EnumValue;
        var valueName = firstValue.Name;
        var description = firstValue.Description;
        var isDeprecated = firstValue.IsDeprecated;
        var deprecationReason = firstValue.DeprecationReason;

        for (var i = 1; i < enumValueGroup.Length; i++)
        {
            var enumValueInfo = enumValueGroup[i];
            description ??= enumValueInfo.EnumValue.Description;

            if (enumValueInfo.EnumValue.IsDeprecated && !isDeprecated)
            {
                isDeprecated = true;
            }

            if (isDeprecated && string.IsNullOrEmpty(deprecationReason))
            {
                deprecationReason = enumValueInfo.EnumValue.DeprecationReason;
            }
        }

        var enumValue = new MutableEnumValue(valueName)
        {
            Description = description,
            IsDeprecated = isDeprecated,
            DeprecationReason = deprecationReason
        };

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = enumValueGroup.Select(g => g.EnumValue).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.Tag].MergeDirectives(enumValue, memberDefinitions, mergedSchema);

                AddFusionEnumValueDirectives(enumValue, enumValueGroup);

                if (enumValueGroup.Any(i => i.EnumValue.HasInaccessibleDirective()))
                {
                    enumValue.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

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
    private MutableInputObjectTypeDefinition MergeInputTypes(
        ImmutableArray<TypeInfo> typeGroup,
        MutableSchemaDefinition mergedSchema)
    {
        var firstType = (MutableInputObjectTypeDefinition)typeGroup[0].Type;
        var typeName = firstType.Name;
        var description = firstType.Description;
        var inputObjectType =
            GetOrCreateType<MutableInputObjectTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        inputObjectType.Description = description;

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = typeGroup.Select(g => g.Type).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.OneOf]
                    .MergeDirectives(inputObjectType, memberDefinitions, mergedSchema);
                _directiveMergers[DirectiveNames.Tag]
                    .MergeDirectives(inputObjectType, memberDefinitions, mergedSchema);

                AddFusionTypeDirectives(inputObjectType, typeGroup);

                if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
                {
                    inputObjectType.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((MutableInputObjectTypeDefinition)i.Type).Fields.AsEnumerable(),
                (i, f) => new InputFieldInfo(f, (MutableInputObjectTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name)
            // Intersection: Field definition count matches type definition count.
            .Where(g => g.Count() == typeGroup.Length);

        foreach (var fieldGroup in fieldGroupByName)
        {
            var mergedField = MergeInputFields([.. fieldGroup], mergedSchema);
            mergedField.DeclaringMember = inputObjectType;
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
    private MutableInputFieldDefinition MergeInputFields(
        ImmutableArray<InputFieldInfo> inputFieldGroup,
        MutableSchemaDefinition mergedSchema)
    {
        var firstField = inputFieldGroup[0].Field;
        var fieldName = firstField.Name;
        var fieldType = firstField.Type;
        var description = firstField.Description;
        var defaultValue = firstField.DefaultValue;
        var isDeprecated = firstField.IsDeprecated;
        var deprecationReason = firstField.DeprecationReason;

        for (var i = 1; i < inputFieldGroup.Length; i++)
        {
            var inputFieldInfo = inputFieldGroup[i];
            fieldType = MostRestrictiveType(fieldType, inputFieldInfo.Field.Type).ExpectInputType();
            description ??= inputFieldInfo.Field.Description;
            defaultValue ??= inputFieldInfo.Field.DefaultValue;

            if (inputFieldInfo.Field.IsDeprecated && !isDeprecated)
            {
                isDeprecated = true;
            }

            if (isDeprecated && string.IsNullOrEmpty(deprecationReason))
            {
                deprecationReason = inputFieldInfo.Field.DeprecationReason;
            }
        }

        var inputField = new MutableInputFieldDefinition(fieldName)
        {
            DefaultValue = defaultValue,
            Description = description,
            IsDeprecated = isDeprecated,
            DeprecationReason = deprecationReason,
            Type = fieldType
                .ReplaceNamedType(_ => GetOrCreateType(mergedSchema, fieldType))
                .ExpectInputType()
        };

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = inputFieldGroup.Select(g => g.Type).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.Tag].MergeDirectives(inputField, memberDefinitions, mergedSchema);

                AddFusionInputFieldDirectives(inputField, inputFieldGroup);

                if (inputFieldGroup.Any(i => i.Field.HasInaccessibleDirective()))
                {
                    inputField.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

        return inputField;
    }

    /// <summary>
    /// Unifies multiple interface definitions (all sharing the same name) into a single composed
    /// interface type.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Interface-Types">
    /// Specification
    /// </seealso>
    private MutableInterfaceTypeDefinition MergeInterfaceTypes(
        ImmutableArray<TypeInfo> typeGroup,
        MutableSchemaDefinition mergedSchema)
    {
        var firstType = typeGroup[0].Type;
        var typeName = firstType.Name;
        var description = firstType.Description;
        var interfaceType = GetOrCreateType<MutableInterfaceTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        interfaceType.Description = description;

        // [InterfaceName: [{InterfaceType, Schema}, ...], ...].
        var interfaceGroupByName = typeGroup
            .SelectMany(
                i => ((MutableInterfaceTypeDefinition)i.Type).Implements.AsEnumerable(),
                (i, it) => new InterfaceInfo(it, i.Schema))
            .GroupBy(i => i.InterfaceType.Name)
            .Where(g => !g.Any(i => i.InterfaceType.HasInaccessibleDirective()))
            .ToArray();

        foreach (var (interfaceName, _) in interfaceGroupByName)
        {
            interfaceType.Implements.Add(
                GetOrCreateType<MutableInterfaceTypeDefinition>(mergedSchema, interfaceName));
        }

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = typeGroup.Select(g => g.Type).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.CacheControl].MergeDirectives(interfaceType, memberDefinitions, mergedSchema);
                _directiveMergers[DirectiveNames.Tag].MergeDirectives(interfaceType, memberDefinitions, mergedSchema);

                AddFusionTypeDirectives(interfaceType, typeGroup);
                AddFusionImplementsDirectives(interfaceType, [.. interfaceGroupByName.SelectMany(g => g)]);

                if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
                {
                    interfaceType.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((MutableInterfaceTypeDefinition)i.Type).Fields.AsEnumerable(),
                (i, f) => new OutputFieldInfo(f, (MutableComplexTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name)
            .ToImmutableArray();

        foreach (var fieldGroup in fieldGroupByName)
        {
            var mergedField = MergeOutputFields([.. fieldGroup], interfaceType, mergedSchema);

            if (mergedField is not null)
            {
                mergedField.DeclaringMember = interfaceType;
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
    private MutableObjectTypeDefinition? MergeObjectTypes(
        ImmutableArray<TypeInfo> typeGroup,
        MutableSchemaDefinition mergedSchema)
    {
        // Filter out internal types.
        typeGroup = [.. typeGroup.Where(i => !((MutableObjectTypeDefinition)i.Type).IsInternal)];

        if (typeGroup.Length == 0)
        {
            return null;
        }

        var firstType = typeGroup[0].Type;
        var typeName = firstType.Name;
        var description = firstType.Description;
        var objectType = GetOrCreateType<MutableObjectTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        objectType.Description = description;

        // [InterfaceName: [{InterfaceType, Schema}, ...], ...].
        var interfaceGroupByName = typeGroup
            .SelectMany(
                i => ((MutableObjectTypeDefinition)i.Type).Implements.AsEnumerable(),
                (i, it) => new InterfaceInfo(it, i.Schema))
            .GroupBy(i => i.InterfaceType.Name)
            .Where(g => !g.Any(i => i.InterfaceType.HasInaccessibleDirective()))
            .ToArray();

        foreach (var (interfaceName, _) in interfaceGroupByName)
        {
            objectType.Implements.Add(
                GetOrCreateType<MutableInterfaceTypeDefinition>(mergedSchema, interfaceName));
        }

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = typeGroup.Select(g => g.Type).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.CacheControl]
                    .MergeDirectives(objectType, memberDefinitions, mergedSchema);
                _directiveMergers[DirectiveNames.Tag]
                    .MergeDirectives(objectType, memberDefinitions, mergedSchema);

                AddFusionTypeDirectives(objectType, typeGroup);
                AddFusionImplementsDirectives(objectType, [.. interfaceGroupByName.SelectMany(g => g)]);

                if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
                {
                    objectType.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

        // [FieldName: [{Field, Type, Schema}, ...], ...].
        var fieldGroupByName = typeGroup
            .SelectMany(
                i => ((MutableObjectTypeDefinition)i.Type).Fields.AsEnumerable(),
                (i, f) => new OutputFieldInfo(f, (MutableComplexTypeDefinition)i.Type, i.Schema))
            .GroupBy(i => i.Field.Name)
            .ToImmutableArray();

        foreach (var fieldGroup in fieldGroupByName)
        {
            var mergedField = MergeOutputFields([.. fieldGroup], objectType, mergedSchema);

            if (mergedField is not null)
            {
                mergedField.DeclaringMember = objectType;
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
    private MutableOutputFieldDefinition? MergeOutputFields(
        ImmutableArray<OutputFieldInfo> fieldGroup,
        MutableComplexTypeDefinition complexType,
        MutableSchemaDefinition mergedSchema)
    {
        // Filter out internal or overridden fields.
        fieldGroup =
            [.. fieldGroup.Where(i => i.Field is { IsInternal: false, IsOverridden: false })];

        if (fieldGroup.Length == 0)
        {
            return null;
        }

        var firstField = fieldGroup[0].Field;
        var fieldName = firstField.Name;
        var fieldType = firstField.Type;
        var description = firstField.Description;
        var isDeprecated = firstField.IsDeprecated;
        var deprecationReason = firstField.DeprecationReason;

        for (var i = 1; i < fieldGroup.Length; i++)
        {
            var fieldInfo = fieldGroup[i];
            fieldType = LeastRestrictiveType(fieldType, fieldInfo.Field.Type).ExpectOutputType();
            description ??= fieldInfo.Field.Description;

            if (fieldInfo.Field.IsDeprecated && !isDeprecated)
            {
                isDeprecated = true;
            }

            if (isDeprecated && string.IsNullOrEmpty(deprecationReason))
            {
                deprecationReason = fieldInfo.Field.DeprecationReason;
            }
        }

        var outputField = new MutableOutputFieldDefinition(fieldName)
        {
            Description = description,
            IsDeprecated = isDeprecated,
            DeprecationReason = deprecationReason,
            Type = fieldType
                .ReplaceNamedType(_ => GetOrCreateType(mergedSchema, fieldType))
                .ExpectOutputType()
        };

        // [ArgumentName: [{Argument, Field, Type, Schema}, ...], ...].
        var argumentGroupByName = fieldGroup
            .SelectMany(
                i => i.Field.Arguments.AsEnumerable(),
                (i, a) => new FieldArgumentInfo(a, i.Field, i.Type, i.Schema))
            .Where(i => !i.Argument.HasRequireDirective)
            .GroupBy(i => i.Argument.Name)
            // Intersection: Argument definition count matches field definition count.
            .Where(g => g.Count() == fieldGroup.Length);

        foreach (var argumentGroup in argumentGroupByName)
        {
            var mergedArgument = MergeArgumentDefinitions([.. argumentGroup], mergedSchema);

            if (mergedArgument is not null)
            {
                mergedArgument.DeclaringMember = outputField;
                outputField.Arguments.Add(mergedArgument);
            }
        }

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = fieldGroup.Select(g => g.Field).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.CacheControl]
                    .MergeDirectives(outputField, memberDefinitions, mergedSchema);
                _directiveMergers[DirectiveNames.McpToolAnnotations]
                    .MergeDirectives(outputField, memberDefinitions, mergedSchema);
                _directiveMergers[DirectiveNames.Tag]
                    .MergeDirectives(outputField, memberDefinitions, mergedSchema);

                AddFusionFieldDirectives(outputField, fieldGroup);
                AddFusionRequiresDirectives(outputField, complexType, [.. fieldGroup]);

                if (fieldGroup.Any(i => i.Field.HasInaccessibleDirective()))
                {
                    outputField.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

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
    private MutableScalarTypeDefinition? MergeScalarTypes(
        ImmutableArray<TypeInfo> typeGroup,
        MutableSchemaDefinition mergedSchema)
    {
        var firstScalar = typeGroup[0].Type;
        var typeName = firstScalar.Name;

        // Built-in Fusion scalar types should not be merged.
        if (FusionBuiltIns.IsBuiltInSourceSchemaScalar(typeName))
        {
            return null;
        }

        var description = firstScalar.Description;
        var scalarType = GetOrCreateType<MutableScalarTypeDefinition>(mergedSchema, typeName);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        scalarType.Description = description;

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = typeGroup.Select(g => g.Type).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.SerializeAs]
                    .MergeDirectives(scalarType, memberDefinitions, mergedSchema);
                _directiveMergers[DirectiveNames.Tag]
                    .MergeDirectives(scalarType, memberDefinitions, mergedSchema);

                AddFusionTypeDirectives(scalarType, typeGroup);

                if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
                {
                    scalarType.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

        return scalarType;
    }

    /// <summary>
    /// Aggregates multiple union type definitions that share the <i>same name</i> into one unified
    /// union type. This process excludes possible types marked with <c>@internal</c>.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Merge-Union-Types">
    /// Specification
    /// </seealso>
    private MutableUnionTypeDefinition MergeUnionTypes(
        ImmutableArray<TypeInfo> typeGroup,
        MutableSchemaDefinition mergedSchema)
    {
        var firstUnion = typeGroup[0].Type;
        var name = firstUnion.Name;
        var description = firstUnion.Description;
        var unionType = GetOrCreateType<MutableUnionTypeDefinition>(mergedSchema, name);

        for (var i = 1; i < typeGroup.Length; i++)
        {
            description ??= typeGroup[i].Type.Description;
        }

        unionType.Description = description;

        // [UnionMemberName: [{MemberType, UnionType, Schema}, ...], ...].
        var unionMemberGroupByName = typeGroup
            .SelectMany(
                i => ((MutableUnionTypeDefinition)i.Type).Types.AsEnumerable(),
                (i, t) => new UnionMemberInfo(t, (MutableUnionTypeDefinition)i.Type, i.Schema))
            .Where(i => !i.MemberType.IsInternal)
            .GroupBy(i => i.MemberType.Name)
            .ToImmutableArray();

        foreach (var (memberName, _) in unionMemberGroupByName)
        {
            unionType.Types.Add(
                GetOrCreateType<MutableObjectTypeDefinition>(mergedSchema, memberName));
        }

        // Add directives.
        _applyDirectiveActions.Add(
            () =>
            {
                var memberDefinitions = typeGroup.Select(g => g.Type).ToImmutableArray<IDirectivesProvider>();
                _directiveMergers[DirectiveNames.CacheControl]
                    .MergeDirectives(unionType, memberDefinitions, mergedSchema);
                _directiveMergers[DirectiveNames.Tag]
                    .MergeDirectives(unionType, memberDefinitions, mergedSchema);

                AddFusionTypeDirectives(unionType, typeGroup);

                foreach (var (_, memberGroup) in unionMemberGroupByName)
                {
                    AddFusionUnionMemberDirectives(unionType, [.. memberGroup]);
                }

                if (typeGroup.Any(i => i.Type.HasInaccessibleDirective()))
                {
                    unionType.Directives.Add(
                        new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionInaccessible]));
                }
            });

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
    private static IType LeastRestrictiveType(
        IType typeA,
        IType typeB)
    {
        var isNullable = !(typeA is NonNullType && typeB is NonNullType);

        if (typeA is NonNullType)
        {
            typeA = typeA.NullableType();
        }

        if (typeB is NonNullType)
        {
            typeB = typeB.NullableType();
        }

        if (typeA is ListType)
        {
            Assert(typeB is ListType);

            var innerTypeA = typeA.InnerType();
            var innerTypeB = typeB.InnerType();
            var innerType = LeastRestrictiveType(innerTypeA, innerTypeB);

            return isNullable
                ? new ListType(innerType)
                : new NonNullType(new ListType(innerType));
        }

        Assert(typeA.Equals(typeB, TypeComparison.Structural));

        return isNullable ? typeA : new NonNullType(typeA);
    }

    /// <summary>
    /// Determines a single input type that strictly honors the constraints of both sources. If
    /// either source requires a non-null value, the merged type also becomes non-null so that no
    /// invalid (e.g., <c>null</c>) data can be introduced at runtime. Conversely, if both sources
    /// allow <c>null</c>, the merged type remains nullable. The same principle applies to list
    /// types, where the more restrictive settings (non-null list or non-null elements) are used.
    /// </summary>
    /// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Most-Restrictive-Type">
    /// Specification
    /// </seealso>
    private static IType MostRestrictiveType(
        IType typeA,
        IType typeB)
    {
        var isNullable = typeA is not NonNullType && typeB is not NonNullType;

        if (typeA is NonNullType)
        {
            typeA = typeA.NullableType();
        }

        if (typeB is NonNullType)
        {
            typeB = typeB.NullableType();
        }

        if (typeA is ListType)
        {
            Assert(typeB is ListType);

            var innerTypeA = typeA.InnerType();
            var innerTypeB = typeB.InnerType();
            var innerType = MostRestrictiveType(innerTypeA, innerTypeB);

            return isNullable
                ? new ListType(innerType)
                : new NonNullType(new ListType(innerType));
        }

        Assert(typeA.Equals(typeB, TypeComparison.Structural));

        return isNullable ? typeA : new NonNullType(typeA);
    }

    private static void SetOperationTypes(MutableSchemaDefinition mergedSchema)
    {
        if (mergedSchema.Types.TryGetType(TypeNames.Query, out var queryType))
        {
            mergedSchema.QueryType = (MutableObjectTypeDefinition?)queryType;
        }

        if (mergedSchema.Types.TryGetType(TypeNames.Mutation, out var mutationType)
            && mutationType is MutableObjectTypeDefinition mutationObjectType)
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
            && subscriptionType is MutableObjectTypeDefinition subscriptionObjectType)
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

    private T GetOrCreateType<T>(MutableSchemaDefinition mergedSchema, string typeName)
        where T : class, INamedTypeSystemMemberDefinition<T>
    {
        if (mergedSchema.Types.TryGetType(typeName, out var existingType))
        {
            return (T)existingType;
        }

        var sourceTypes = _schemas.SelectMany(s => s.Types.Where(t => t.Name == typeName)).ToArray();
        var allInternal =
            sourceTypes.Length != 0
            && sourceTypes.All(t => t is MutableObjectTypeDefinition { IsInternal: true });

        T newType;
        if (allInternal)
        {
            newType = (T)(ITypeDefinition)new InternalMutableObjectTypeDefinition(typeName);
        }
        else
        {
            newType = T.Create(typeName);
        }

        mergedSchema.Types.Add((ITypeDefinition)newType);

        return newType;
    }

    private ITypeDefinition GetOrCreateType(
        MutableSchemaDefinition mergedSchema,
        IType sourceType)
    {
        return sourceType switch
        {
            MutableEnumTypeDefinition e
                => GetOrCreateType<MutableEnumTypeDefinition>(mergedSchema, e.Name),
            MutableInputObjectTypeDefinition i
                => GetOrCreateType<MutableInputObjectTypeDefinition>(mergedSchema, i.Name),
            MutableInterfaceTypeDefinition i
                => GetOrCreateType<MutableInterfaceTypeDefinition>(mergedSchema, i.Name),
            ListType l
                => GetOrCreateType(mergedSchema, l.ElementType),
            MissingType m
                => new MissingType(m.Name),
            NonNullType n
                => GetOrCreateType(mergedSchema, n.NullableType),
            MutableObjectTypeDefinition o
                => GetOrCreateType<MutableObjectTypeDefinition>(mergedSchema, o.Name),
            MutableScalarTypeDefinition s
                => GetOrCreateType<MutableScalarTypeDefinition>(mergedSchema, s.Name),
            MutableUnionTypeDefinition u
                => GetOrCreateType<MutableUnionTypeDefinition>(mergedSchema, u.Name),
            _
                => throw new ArgumentOutOfRangeException(nameof(sourceType))
        };
    }

    private void AddFusionEnumValueDirectives(
        MutableEnumValue enumValue,
        ImmutableArray<EnumValueInfo> enumValueGroup)
    {
        foreach (var (_, _, sourceSchema) in enumValueGroup)
        {
            var schema = new EnumValueNode(_schemaConstantNames[sourceSchema.Name]);

            enumValue.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionEnumValue],
                    new ArgumentAssignment(ArgumentNames.Schema, schema)));
        }
    }

    private void AddFusionFieldDirectives(
        MutableOutputFieldDefinition field,
        ImmutableArray<OutputFieldInfo> fieldGroup)
    {
        foreach (var (sourceField, _, sourceSchema) in fieldGroup)
        {
            List<ArgumentAssignment> arguments =
            [
                new(
                    ArgumentNames.Schema,
                    new EnumValueNode(_schemaConstantNames[sourceSchema.Name]))
            ];

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

            if (sourceField.IsExternal)
            {
                arguments.Add(new ArgumentAssignment(ArgumentNames.Partial, true));
            }

            field.Directives.Add(
                new Directive(_fusionDirectiveDefinitions[DirectiveNames.FusionField], arguments));
        }
    }

    private void AddFusionImplementsDirectives(
        MutableComplexTypeDefinition complexType,
        ImmutableArray<InterfaceInfo> interfaceGroup)
    {
        foreach (var (sourceInterface, sourceSchema) in interfaceGroup)
        {
            complexType.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionImplements],
                    new ArgumentAssignment(
                        ArgumentNames.Schema,
                        new EnumValueNode(_schemaConstantNames[sourceSchema.Name])),
                    new ArgumentAssignment(ArgumentNames.Interface, sourceInterface.Name)));
        }
    }

    private void AddFusionInputFieldDirectives(
        MutableInputFieldDefinition argument,
        ImmutableArray<FieldArgumentInfo> argumentGroup)
    {
        foreach (var (sourceArgument, _, _, sourceSchema) in argumentGroup)
        {
            List<ArgumentAssignment> arguments =
            [
                new(
                    ArgumentNames.Schema,
                    new EnumValueNode(_schemaConstantNames[sourceSchema.Name]))
            ];

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
        MutableInputFieldDefinition inputField,
        ImmutableArray<InputFieldInfo> inputFieldGroup)
    {
        foreach (var (sourceInputField, _, sourceSchema) in inputFieldGroup)
        {
            List<ArgumentAssignment> arguments =
            [
                new(
                    ArgumentNames.Schema,
                    new EnumValueNode(_schemaConstantNames[sourceSchema.Name]))
            ];

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

    private void AddFusionRequiresDirectives(
        MutableOutputFieldDefinition field,
        MutableComplexTypeDefinition complexType,
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
                var schemaArgument = new EnumValueNode(_schemaConstantNames[sourceSchema.Name]);
                var requiresMap = map.Where(f => f is not null);
                var selectedValues =
                    requiresMap.Select(a => new FieldSelectionMapParser(a).Parse());
                var selectedValueToSelectionSetRewriter =
                    GetSelectedValueToSelectionSetRewriter(sourceSchema);
                var selectionSets = selectedValues
                    .Select(
                        s =>
                            selectedValueToSelectionSetRewriter
                                .Rewrite(s, complexType))
                    .ToImmutableArray();
                var mergedSelectionSet = selectionSets.Length == 1
                    ? selectionSets[0]
                    : GetMergeSelectionSetRewriter(sourceSchema).Merge(selectionSets, complexType);
                var requirementsArgument =
                    mergedSelectionSet.ToString(indented: false).AsSpan()[2..^2].ToString();

                var fieldArgument =
                    s_fieldDefinitionRewriter
                        .Rewrite(sourceField.ToSyntaxNode())!
                        .ToString(indented: false);

                var mapArgument = new ListValueNode(
                    map.ConvertAll<IValueNode>(
                        v => v is null ? NullValueNode.Default : new StringValueNode(v)));

                field.Directives.Add(
                    new Directive(
                        _fusionDirectiveDefinitions[DirectiveNames.FusionRequires],
                        new ArgumentAssignment(ArgumentNames.Schema, schemaArgument),
                        new ArgumentAssignment(ArgumentNames.Requirements, requirementsArgument),
                        new ArgumentAssignment(ArgumentNames.Field, fieldArgument),
                        new ArgumentAssignment(ArgumentNames.Map, mapArgument)));
            }
        }
    }

    private void AddFusionTypeDirectives(
        IMutableTypeDefinition type,
        ImmutableArray<TypeInfo> typeGroup)
    {
        foreach (var (_, sourceSchema) in typeGroup)
        {
            var schema = new EnumValueNode(_schemaConstantNames[sourceSchema.Name]);

            type.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionType],
                    new ArgumentAssignment(ArgumentNames.Schema, schema)));
        }
    }

    private void AddFusionUnionMemberDirectives(
        MutableUnionTypeDefinition unionType,
        ImmutableArray<UnionMemberInfo> unionMemberGroup)
    {
        foreach (var (sourceMemberType, _, sourceSchema) in unionMemberGroup)
        {
            var schema = new EnumValueNode(_schemaConstantNames[sourceSchema.Name]);

            unionType.Directives.Add(
                new Directive(
                    _fusionDirectiveDefinitions[DirectiveNames.FusionUnionMember],
                    new ArgumentAssignment(ArgumentNames.Schema, schema),
                    new ArgumentAssignment(ArgumentNames.Member, sourceMemberType.Name)));
        }
    }

    private FrozenDictionary<string, ITypeDefinition> CreateFusionTypeDefinitions()
    {
        return new Dictionary<string, ITypeDefinition>
        {
            // Scalar type definitions.
            {
                TypeNames.FusionFieldDefinition,
                new FusionFieldDefinitionMutableScalarTypeDefinition()
            },
            {
                TypeNames.FusionFieldSelectionMap,
                new FusionFieldSelectionMapMutableScalarTypeDefinition()
            },
            {
                TypeNames.FusionFieldSelectionPath,
                new FusionFieldSelectionPathMutableScalarTypeDefinition()
            },
            {
                TypeNames.FusionFieldSelectionSet,
                new FusionFieldSelectionSetMutableScalarTypeDefinition()
            },
            // Enum type definitions.
            {
                TypeNames.FusionSchema,
                new FusionSchemaMutableEnumTypeDefinition(_schemaConstantNames)
            }
        }.ToFrozenDictionary();
    }

    private FrozenDictionary<string, MutableDirectiveDefinition> CreateFusionDirectiveDefinitions()
    {
        var schemaEnumType =
            (MutableEnumTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionSchema];
        var fieldDefinitionType =
            (MutableScalarTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionFieldDefinition];
        var fieldSelectionMapType =
            (MutableScalarTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionFieldSelectionMap];
        var fieldSelectionPathType =
            (MutableScalarTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionFieldSelectionPath];
        var fieldSelectionSetType =
            (MutableScalarTypeDefinition)_fusionTypeDefinitions[TypeNames.FusionFieldSelectionSet];
        var stringType = BuiltIns.String.Create();
        var booleanType = BuiltIns.Boolean.Create();

        return new Dictionary<string, MutableDirectiveDefinition>
        {
            {
                DirectiveNames.FusionEnumValue,
                new FusionEnumValueMutableDirectiveDefinition(schemaEnumType)
            },
            {
                DirectiveNames.FusionField,
                new FusionFieldMutableDirectiveDefinition(
                    schemaEnumType,
                    stringType,
                    fieldSelectionSetType,
                    booleanType)
            },
            {
                DirectiveNames.FusionImplements,
                new FusionImplementsMutableDirectiveDefinition(schemaEnumType, stringType)
            },
            {
                DirectiveNames.FusionInaccessible,
                new FusionInaccessibleMutableDirectiveDefinition()
            },
            {
                DirectiveNames.FusionInputField,
                new FusionInputFieldMutableDirectiveDefinition(schemaEnumType, stringType)
            },
            {
                DirectiveNames.FusionLookup,
                new FusionLookupMutableDirectiveDefinition(
                    schemaEnumType,
                    fieldSelectionSetType,
                    fieldDefinitionType,
                    fieldSelectionMapType,
                    fieldSelectionPathType,
                    booleanType)
            },
            {
                DirectiveNames.FusionRequires,
                new FusionRequiresMutableDirectiveDefinition(
                    schemaEnumType,
                    fieldSelectionSetType,
                    fieldDefinitionType,
                    fieldSelectionMapType)
            },
            {
                DirectiveNames.FusionSchemaMetadata,
                new FusionSchemaMetadataMutableDirectiveDefinition(stringType)
            },
            {
                DirectiveNames.FusionType,
                new FusionTypeMutableDirectiveDefinition(schemaEnumType)
            },
            {
                DirectiveNames.FusionUnionMember,
                new FusionUnionMemberMutableDirectiveDefinition(schemaEnumType, stringType)
            }
        }.ToFrozenDictionary();
    }

    private void AddFusionDefinitions(MutableSchemaDefinition mergedSchema)
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

    private ValueSelectionToSelectionSetRewriter GetSelectedValueToSelectionSetRewriter(
        MutableSchemaDefinition schema)
    {
        if (_selectedValueToSelectionSetRewriters.TryGetValue(schema.Name, out var rewriter))
        {
            return rewriter;
        }

        rewriter = new ValueSelectionToSelectionSetRewriter(schema);
        _selectedValueToSelectionSetRewriters.Add(schema.Name, rewriter);

        return rewriter;
    }

    private MergeSelectionSetRewriter GetMergeSelectionSetRewriter(MutableSchemaDefinition schema)
    {
        if (_mergeSelectionSetRewriters.TryGetValue(schema.Name, out var rewriter))
        {
            return rewriter;
        }

        rewriter = new MergeSelectionSetRewriter(schema);
        _mergeSelectionSetRewriters.Add(schema.Name, rewriter);

        return rewriter;
    }

    private static void Assert(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException();
        }
    }
}
