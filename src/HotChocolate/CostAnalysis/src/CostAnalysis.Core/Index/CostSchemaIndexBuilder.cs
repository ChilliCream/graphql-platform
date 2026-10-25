using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Builds an index of the schema metadata used by cost analysis.
/// </summary>
internal static class CostSchemaIndexBuilder
{
    public static CostSchemaIndex Build(ISchemaDefinition schema, CostSchemaIndexOptions options)
    {
        var defaultListSize = options.DefaultListSize;

        if (double.IsNaN(defaultListSize) || defaultListSize < 0)
        {
            throw ThrowHelper.InvalidCostOptionValue(
                nameof(CostSchemaIndexOptions.DefaultListSize),
                defaultListSize);
        }

        var caseBudget = options.CaseBudget;
        var caseBudgetExceededBehavior = options.CaseBudgetExceededBehavior;
        var objectTypeIndex = IndexObjectTypes(schema, out var objectTypesByIndex);
        var objectTypeCount = objectTypesByIndex.Length;
        var typeWeights = new Dictionary<string, double>();
        var possibleTypes = new Dictionary<string, PossibleTypeSet>();

        // Read concrete type weights before computing weights for interfaces and unions.
        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case IObjectTypeDefinition objectType:
                    typeWeights.Add(objectType.Name, ReadTypeWeight(objectType, defaultWeight: 1.0));
                    possibleTypes.Add(
                        objectType.Name,
                        PossibleTypeSet.Create(objectTypeCount, [objectTypeIndex[objectType.Name]]));
                    break;

                case IScalarTypeDefinition scalarType:
                    typeWeights.Add(scalarType.Name, ReadTypeWeight(scalarType, defaultWeight: 0.0));
                    break;

                case IEnumTypeDefinition enumType:
                    typeWeights.Add(enumType.Name, ReadTypeWeight(enumType, defaultWeight: 0.0));
                    break;
            }
        }

        // Abstract types use the largest weight among their possible object types.
        // Use the schema's possible-type lookup so inaccessible Fusion types are excluded.
        var memberIndices = new List<int>();

        foreach (var type in schema.Types)
        {
            if (type is not (IInterfaceTypeDefinition or IUnionTypeDefinition))
            {
                continue;
            }

            memberIndices.Clear();
            var hasMember = false;
            var maxWeight = 0.0;

            foreach (var possibleType in schema.GetPossibleTypes(type))
            {
                memberIndices.Add(objectTypeIndex[possibleType.Name]);
                var memberWeight = typeWeights[possibleType.Name];

                if (!hasMember || memberWeight > maxWeight)
                {
                    maxWeight = memberWeight;
                }

                hasMember = true;
            }

            typeWeights.Add(type.Name, hasMember ? maxWeight : 1.0);
            possibleTypes.Add(
                type.Name,
                PossibleTypeSet.Create(objectTypeCount, CollectionsMarshal.AsSpan(memberIndices)));
        }

        var listSizeRequireOneDefault = ResolveListSizeRequireOneDefault(schema);
        var fieldWeights = new Dictionary<FieldKey, double>();
        var listSizeMetadata = new Dictionary<FieldKey, ListSizeMetadata>();
        var argumentWeights = new Dictionary<ArgumentKey, double>();
        var inputFieldWeights = new Dictionary<FieldKey, double>();
        var fieldArguments = new Dictionary<FieldKey, ImmutableArray<InputValueMetadata>>();
        var fieldSemanticIds = new Dictionary<IOutputFieldDefinition, int>(
            ReferenceEqualityComparer.Instance);
        var semanticFieldIds = new Dictionary<FieldSemanticIdentity, int>(
            FieldSemanticIdentityComparer.Instance);
        var inputObjectFields = new Dictionary<string, ImmutableArray<InputValueMetadata>>();

        foreach (var type in schema.Types)
        {
            switch (type)
            {
                case IComplexTypeDefinition complexType:
                    IndexOutputFields(
                        complexType,
                        listSizeRequireOneDefault,
                        fieldWeights,
                        listSizeMetadata,
                        argumentWeights,
                        fieldArguments,
                        fieldSemanticIds,
                        semanticFieldIds,
                        typeWeights);
                    break;

                case IInputObjectTypeDefinition inputObjectType:
                    var fields = ImmutableArray.CreateBuilder<InputValueMetadata>(inputObjectType.Fields.Count);

                    foreach (var field in inputObjectType.Fields)
                    {
                        inputFieldWeights.Add(
                            new FieldKey(inputObjectType.Name, field.Name),
                            ReadInputValueWeight(field));
                        fields.Add(CreateInputValueMetadata(field));
                    }

                    inputObjectFields.Add(inputObjectType.Name, fields.MoveToImmutable());

                    break;
            }
        }

        var directiveArguments = new Dictionary<string, ImmutableArray<DirectiveArgumentDefinition>>();
        var directiveArgumentMetadata = new Dictionary<string, ImmutableArray<InputValueMetadata>>();

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            var builder = ImmutableArray.CreateBuilder<DirectiveArgumentDefinition>(
                directiveDefinition.Arguments.Count);
            var metadataBuilder = ImmutableArray.CreateBuilder<InputValueMetadata>(directiveDefinition.Arguments.Count);

            foreach (var argument in directiveDefinition.Arguments)
            {
                builder.Add(new DirectiveArgumentDefinition(
                    argument.Name,
                    ReadInputValueWeight(argument),
                    argument.DefaultValue is not null));
                metadataBuilder.Add(CreateInputValueMetadata(argument));
            }

            directiveArguments.Add(directiveDefinition.Name, builder.MoveToImmutable());
            directiveArgumentMetadata.Add(directiveDefinition.Name, metadataBuilder.MoveToImmutable());
        }

        schema.TryGetOperationType(OperationType.Query, out var queryType);
        schema.TryGetOperationType(OperationType.Mutation, out var mutationType);
        schema.TryGetOperationType(OperationType.Subscription, out var subscriptionType);

        return new CostSchemaIndex(
            defaultListSize,
            caseBudget,
            caseBudgetExceededBehavior,
            queryType?.Name,
            mutationType?.Name,
            subscriptionType?.Name,
            objectTypeIndex,
            objectTypesByIndex,
            possibleTypes.ToFrozenDictionary(),
            typeWeights.ToFrozenDictionary(),
            fieldWeights.ToFrozenDictionary(),
            listSizeMetadata.ToFrozenDictionary(),
            argumentWeights.ToFrozenDictionary(),
            inputFieldWeights.ToFrozenDictionary(),
            fieldArguments.ToFrozenDictionary(),
            fieldSemanticIds.ToFrozenDictionary(ReferenceEqualityComparer.Instance),
            inputObjectFields.ToFrozenDictionary(),
            directiveArguments.ToFrozenDictionary(),
            directiveArgumentMetadata.ToFrozenDictionary());
    }

    private static FrozenDictionary<string, int> IndexObjectTypes(
        ISchemaDefinition schema,
        out IComplexTypeDefinition[] objectTypesByIndex)
    {
        var index = new Dictionary<string, int>();
        var byIndex = new List<IComplexTypeDefinition>();

        foreach (var type in schema.Types)
        {
            if (type is IObjectTypeDefinition objectType)
            {
                index.Add(objectType.Name, index.Count);
                byIndex.Add(objectType);
            }
        }

        objectTypesByIndex = [.. byIndex];
        return index.ToFrozenDictionary();
    }

    private static void IndexOutputFields(
        IComplexTypeDefinition type,
        bool listSizeRequireOneDefault,
        Dictionary<FieldKey, double> fieldWeights,
        Dictionary<FieldKey, ListSizeMetadata> listSizeMetadata,
        Dictionary<ArgumentKey, double> argumentWeights,
        Dictionary<FieldKey, ImmutableArray<InputValueMetadata>> fieldArguments,
        Dictionary<IOutputFieldDefinition, int> fieldSemanticIds,
        Dictionary<FieldSemanticIdentity, int> semanticFieldIds,
        Dictionary<string, double> typeWeights)
    {
        foreach (var field in type.Fields)
        {
            var key = new FieldKey(type.Name, field.Name);
            var fieldWeight = ReadFieldWeight(field);
            fieldWeights.Add(key, fieldWeight);

            var listSize = ReadListSizeMetadata(field, listSizeRequireOneDefault);

            if (listSize is not null)
            {
                listSizeMetadata.Add(key, listSize);
            }

            var arguments = ImmutableArray.CreateBuilder<InputValueMetadata>(field.Arguments.Count);

            foreach (var argument in field.Arguments)
            {
                argumentWeights.Add(
                    new ArgumentKey(type.Name, field.Name, argument.Name),
                    ReadInputValueWeight(argument));
                arguments.Add(CreateInputValueMetadata(argument));
            }

            var argumentMetadata = arguments.MoveToImmutable();
            fieldArguments.Add(key, argumentMetadata);

            var identity = new FieldSemanticIdentity(
                field.Type,
                BitConverter.DoubleToInt64Bits(fieldWeight),
                BitConverter.DoubleToInt64Bits(typeWeights[field.Type.NamedType().Name]),
                listSize,
                argumentMetadata);

            if (!semanticFieldIds.TryGetValue(identity, out var semanticId))
            {
                semanticId = semanticFieldIds.Count;
                semanticFieldIds.Add(identity, semanticId);
            }

            fieldSemanticIds.Add(field, semanticId);
        }
    }

    private static InputValueMetadata CreateInputValueMetadata(IInputValueDefinition value)
        => new(value.Name, ReadInputValueWeight(value), value.Type.NamedType().Name, value.DefaultValue);

    /// <summary>
    /// Gets a type's explicit cost weight, or <paramref name="defaultWeight"/> when none is declared.
    /// </summary>
    private static double ReadTypeWeight(ITypeDefinition type, double defaultWeight)
    {
        var directive = type.Directives.FirstOrDefault(DirectiveNames.Cost.Name);
        return directive is null ? defaultWeight : ReadWeight(directive, type.Coordinate);
    }

    /// <summary>
    /// Gets a field's explicit cost weight, or 1.0 for a composite return type and 0.0 otherwise.
    /// List fields use the kind of their element type.
    /// </summary>
    private static double ReadFieldWeight(IOutputFieldDefinition field)
    {
        var directive = field.Directives.FirstOrDefault(DirectiveNames.Cost.Name);

        if (directive is not null)
        {
            return ReadWeight(directive, field.Coordinate);
        }

        return field.Type.NamedType().Kind is TypeKind.Object or TypeKind.Interface or TypeKind.Union
            ? 1.0
            : 0.0;
    }

    /// <summary>
    /// Gets an argument or input field's explicit cost weight.
    /// The default is 1.0 for input objects and 0.0 otherwise. Lists use their element type.
    /// </summary>
    private static double ReadInputValueWeight(IInputValueDefinition value)
    {
        var directive = value.Directives.FirstOrDefault(DirectiveNames.Cost.Name);

        if (directive is not null)
        {
            return ReadWeight(directive, value.Coordinate);
        }

        return value.Type.NamedType().Kind is TypeKind.InputObject ? 1.0 : 0.0;
    }

    private static double ReadWeight(IDirective directive, SchemaCoordinate coordinate)
    {
        if (!directive.Arguments.TryGetValue(DirectiveNames.Cost.Arguments.Weight, out var value))
        {
            throw ThrowHelper.InvalidCostWeight(coordinate, NullValueNode.Default);
        }

        var weight = value switch
        {
            StringValueNode stringValue => ParseWeightString(stringValue.Value, coordinate, value),
            IntValueNode intValue => intValue.ToDouble(),
            FloatValueNode floatValue => floatValue.ToDouble(),
            _ => throw ThrowHelper.InvalidCostWeight(coordinate, value)
        };

        if (!double.IsFinite(weight))
        {
            throw ThrowHelper.InvalidCostWeight(coordinate, value);
        }

        return weight;
    }

    private static double ParseWeightString(string raw, SchemaCoordinate coordinate, IValueNode value)
    {
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
        {
            throw ThrowHelper.InvalidCostWeight(coordinate, value);
        }

        return weight;
    }

    private static ListSizeMetadata? ReadListSizeMetadata(IOutputFieldDefinition field, bool requireOneDefault)
    {
        var directive = field.Directives.FirstOrDefault(DirectiveNames.ListSize.Name);

        if (directive is null)
        {
            return null;
        }

        var coordinate = field.Coordinate;

        return new ListSizeMetadata(
            ReadAssumedSize(directive, coordinate),
            ReadStringList(directive, DirectiveNames.ListSize.Arguments.SlicingArguments, coordinate),
            ReadOptionalNumber(directive, DirectiveNames.ListSize.Arguments.SlicingArgumentDefaultValue, coordinate),
            ReadStringList(directive, DirectiveNames.ListSize.Arguments.SizedFields, coordinate),
            ReadRequireOneSlicingArgument(directive, requireOneDefault, coordinate));
    }

    /// <summary>
    /// Gets the directive definition's default for <c>requireOneSlicingArgument</c>,
    /// or <see langword="true"/> when no default is declared.
    /// </summary>
    private static bool ResolveListSizeRequireOneDefault(ISchemaDefinition schema)
    {
        if (schema.DirectiveDefinitions.TryGetDirective(DirectiveNames.ListSize.Name, out var definition)
            && definition.Arguments.TryGetField(
                DirectiveNames.ListSize.Arguments.RequireOneSlicingArgument,
                out var argument)
            && argument.DefaultValue is BooleanValueNode defaultValue)
        {
            return defaultValue.Value;
        }

        return true;
    }

    private static bool ReadRequireOneSlicingArgument(
        IDirective directive,
        bool requireOneDefault,
        SchemaCoordinate coordinate)
    {
        const string argumentName = DirectiveNames.ListSize.Arguments.RequireOneSlicingArgument;

        if (!directive.Arguments.TryGetValue(argumentName, out var value))
        {
            return requireOneDefault;
        }

        if (value is BooleanValueNode booleanValue)
        {
            return booleanValue.Value;
        }

        throw ThrowHelper.InvalidListSizeArgument(
            coordinate,
            argumentName,
            value);
    }

    private static double? ReadAssumedSize(IDirective directive, SchemaCoordinate coordinate)
    {
        const string argumentName = DirectiveNames.ListSize.Arguments.AssumedSize;

        if (!directive.Arguments.TryGetValue(argumentName, out var value))
        {
            return null;
        }

        if (value is not IntValueNode intValue || intValue.ToDouble() < 0.0)
        {
            throw ThrowHelper.InvalidListSizeArgument(
                coordinate,
                argumentName,
                value);
        }

        return intValue.ToDouble();
    }

    private static double? ReadOptionalNumber(IDirective directive, string argumentName, SchemaCoordinate coordinate)
    {
        if (!directive.Arguments.TryGetValue(argumentName, out var value) || value is NullValueNode)
        {
            return null;
        }

        return value switch
        {
            IntValueNode intValue => intValue.ToDouble(),
            FloatValueNode floatValue => floatValue.ToDouble(),
            _ => throw ThrowHelper.InvalidListSizeArgument(coordinate, argumentName, value)
        };
    }

    private static ImmutableArray<string> ReadStringList(
        IDirective directive,
        string argumentName,
        SchemaCoordinate coordinate)
    {
        if (!directive.Arguments.TryGetValue(argumentName, out var value) || value is NullValueNode)
        {
            return [];
        }

        if (value is StringValueNode single)
        {
            ValidateName(single.Value, coordinate, argumentName, value);
            return [single.Value];
        }

        if (value is not ListValueNode listValue)
        {
            throw ThrowHelper.InvalidListSizeArgument(coordinate, argumentName, value);
        }

        var builder = ImmutableArray.CreateBuilder<string>(listValue.Items.Count);

        foreach (var item in listValue.Items)
        {
            if (item is not StringValueNode stringValue)
            {
                throw ThrowHelper.InvalidListSizeArgument(coordinate, argumentName, value);
            }

            ValidateName(stringValue.Value, coordinate, argumentName, value);
            builder.Add(stringValue.Value);
        }

        return builder.MoveToImmutable();
    }

    private static void ValidateName(
        string name,
        SchemaCoordinate coordinate,
        string argumentName,
        IValueNode value)
    {
        if (!name.IsValidGraphQLName())
        {
            throw ThrowHelper.InvalidListSizeArgument(coordinate, argumentName, value);
        }
    }
}
