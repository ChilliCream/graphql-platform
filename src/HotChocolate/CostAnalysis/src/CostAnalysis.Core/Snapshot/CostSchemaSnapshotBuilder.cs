using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Builds a <see cref="CostSchemaSnapshot"/> from an <see cref="ISchemaDefinition"/>. This is
/// the only place the cost engine reads the schema directly; everything downstream (compiling
/// and evaluating a <see cref="CostPlan"/>) touches only the resulting snapshot.
/// </summary>
internal static class CostSchemaSnapshotBuilder
{
    public static CostSchemaSnapshot Build(ISchemaDefinition schema, CostEngineOptions options)
    {
        var objectTypeIndex = IndexObjectTypes(schema, out var objectTypeCount);
        var typeWeights = new Dictionary<string, double>();
        var possibleTypes = new Dictionary<string, PossibleTypeSet>();

        // Pass 1: object/scalar/enum types can be read directly; object types also get their
        // trivial possible-type set (an object type's only possible type is itself, so
        // GetPossibleTypes is never called for them; R-POSSIBLE-TYPES).
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

        // Pass 2: interface/union possible-type sets, read through the schema's own
        // GetPossibleTypes (the public, accessible-only view on Fusion; R-POSSIBLE-TYPES), and
        // their type weight, the signed max over member object types already read in pass 1.
        // An interface's or union's own @cost usage is never read: it is not a valid directive
        // location for the spec directive, and the oracle's abstract-type weight consults only
        // member object types (hc-3-mmh.7 edge rule (d)).
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

            typeWeights.Add(type.Name, hasMember ? maxWeight : 0.0);
            possibleTypes.Add(
                type.Name,
                PossibleTypeSet.Create(objectTypeCount, CollectionsMarshal.AsSpan(memberIndices)));
        }

        // Pass 3: output fields (weight, @listSize, argument weights) and input fields.
        var listSizeRequireOneDefault = ResolveListSizeRequireOneDefault(schema);
        var fieldWeights = new Dictionary<FieldKey, double>();
        var listSizeMetadata = new Dictionary<FieldKey, ListSizeMetadata>();
        var argumentWeights = new Dictionary<ArgumentKey, double>();
        var inputFieldWeights = new Dictionary<FieldKey, double>();

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
                        argumentWeights);
                    break;

                case IInputObjectTypeDefinition inputObjectType:
                    foreach (var field in inputObjectType.Fields)
                    {
                        inputFieldWeights.Add(
                            new FieldKey(inputObjectType.Name, field.Name),
                            ReadInputValueWeight(field));
                    }

                    break;
            }
        }

        // Pass 4: directive-definition arguments (query-directive pricing; R-DIRECTIVE-ARG-COST).
        var directiveArgumentWeights = new Dictionary<DirectiveArgumentKey, double>();

        foreach (var directiveDefinition in schema.DirectiveDefinitions)
        {
            foreach (var argument in directiveDefinition.Arguments)
            {
                directiveArgumentWeights.Add(
                    new DirectiveArgumentKey(directiveDefinition.Name, argument.Name),
                    ReadInputValueWeight(argument));
            }
        }

        return new CostSchemaSnapshot(
            options,
            objectTypeIndex,
            possibleTypes.ToFrozenDictionary(),
            typeWeights.ToFrozenDictionary(),
            fieldWeights.ToFrozenDictionary(),
            listSizeMetadata.ToFrozenDictionary(),
            argumentWeights.ToFrozenDictionary(),
            inputFieldWeights.ToFrozenDictionary(),
            directiveArgumentWeights.ToFrozenDictionary());
    }

    private static FrozenDictionary<string, int> IndexObjectTypes(
        ISchemaDefinition schema,
        out int objectTypeCount)
    {
        var index = new Dictionary<string, int>();

        foreach (var type in schema.Types)
        {
            if (type is IObjectTypeDefinition objectType)
            {
                index.Add(objectType.Name, index.Count);
            }
        }

        objectTypeCount = index.Count;
        return index.ToFrozenDictionary();
    }

    private static void IndexOutputFields(
        IComplexTypeDefinition type,
        bool listSizeRequireOneDefault,
        Dictionary<FieldKey, double> fieldWeights,
        Dictionary<FieldKey, ListSizeMetadata> listSizeMetadata,
        Dictionary<ArgumentKey, double> argumentWeights)
    {
        foreach (var field in type.Fields)
        {
            var key = new FieldKey(type.Name, field.Name);
            fieldWeights.Add(key, ReadFieldWeight(field));

            var listSize = ReadListSizeMetadata(field, listSizeRequireOneDefault);

            if (listSize is not null)
            {
                listSizeMetadata.Add(key, listSize);
            }

            foreach (var argument in field.Arguments)
            {
                argumentWeights.Add(
                    new ArgumentKey(type.Name, field.Name, argument.Name),
                    ReadInputValueWeight(argument));
            }
        }
    }

    /// <summary>
    /// Reads a named type's own weight: an explicit <c>@cost</c> usage wins, otherwise the
    /// caller's kind default (composite 1.0, leaf 0.0; hc-3-mmh.7 item 1).
    /// </summary>
    private static double ReadTypeWeight(ITypeDefinition type, double defaultWeight)
    {
        var directive = type.Directives.FirstOrDefault(DirectiveNames.Cost.Name);
        return directive is null ? defaultWeight : ReadWeight(directive, type.Coordinate);
    }

    /// <summary>
    /// Reads an output field's own weight: an explicit <c>@cost</c> usage wins, otherwise 1.0
    /// when the field's named return type is an object, interface or union, else 0.0 (a list of
    /// scalars therefore defaults to 0.0; hc-3-mmh.4, hc-3-mmh.7 item 1).
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
    /// Reads an argument's or input field's own weight: an explicit <c>@cost</c> usage wins,
    /// otherwise 1.0 when the named input type is an input object, else 0.0 (hc-3-mmh.7 item 1).
    /// Shared by output-field arguments, input-object fields and directive-definition arguments,
    /// all of which follow the same rule.
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
            ReadOptionalNumber(directive, DirectiveNames.ListSize.Arguments.AssumedSize, coordinate),
            ReadStringList(directive, DirectiveNames.ListSize.Arguments.SlicingArguments, coordinate),
            ReadOptionalNumber(directive, DirectiveNames.ListSize.Arguments.SlicingArgumentDefaultValue, coordinate),
            ReadStringList(directive, DirectiveNames.ListSize.Arguments.SizedFields, coordinate),
            ReadRequireOneSlicingArgument(directive, requireOneDefault));
    }

    /// <summary>
    /// Resolves the effective <c>requireOneSlicingArgument</c> default for an omitted usage:
    /// the <c>@listSize</c> directive definition's declared default when one exists, else the
    /// spec default <see langword="true"/> (R-REQUIRE-ONE, R-REQUIRE-ONE-DEFAULT). Computed once
    /// per schema since it never varies across usages.
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

    private static bool ReadRequireOneSlicingArgument(IDirective directive, bool requireOneDefault)
    {
        if (directive.Arguments.TryGetValue(
                DirectiveNames.ListSize.Arguments.RequireOneSlicingArgument,
                out var value)
            && value is BooleanValueNode booleanValue)
        {
            return booleanValue.Value;
        }

        return requireOneDefault;
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

            builder.Add(stringValue.Value);
        }

        return builder.MoveToImmutable();
    }
}
