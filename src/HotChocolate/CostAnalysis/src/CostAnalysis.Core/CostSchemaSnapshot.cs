using System.Collections.Frozen;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, per-schema snapshot of a schema's cost-relevant metadata
/// that a <see cref="CostPlan"/> is compiled and evaluated against.
/// </summary>
public sealed class CostSchemaSnapshot
{
    private readonly FrozenDictionary<string, int> _objectTypeIndex;
    private readonly FrozenDictionary<string, PossibleTypeSet> _possibleTypes;
    private readonly FrozenDictionary<string, double> _typeWeights;
    private readonly FrozenDictionary<FieldKey, double> _fieldWeights;
    private readonly FrozenDictionary<FieldKey, ListSizeMetadata> _listSizeMetadata;
    private readonly FrozenDictionary<ArgumentKey, double> _argumentWeights;
    private readonly FrozenDictionary<FieldKey, double> _inputFieldWeights;
    private readonly FrozenDictionary<DirectiveArgumentKey, double> _directiveArgumentWeights;

    internal CostSchemaSnapshot(
        CostEngineOptions options,
        FrozenDictionary<string, int> objectTypeIndex,
        FrozenDictionary<string, PossibleTypeSet> possibleTypes,
        FrozenDictionary<string, double> typeWeights,
        FrozenDictionary<FieldKey, double> fieldWeights,
        FrozenDictionary<FieldKey, ListSizeMetadata> listSizeMetadata,
        FrozenDictionary<ArgumentKey, double> argumentWeights,
        FrozenDictionary<FieldKey, double> inputFieldWeights,
        FrozenDictionary<DirectiveArgumentKey, double> directiveArgumentWeights)
    {
        Options = options;
        _objectTypeIndex = objectTypeIndex;
        _possibleTypes = possibleTypes;
        _typeWeights = typeWeights;
        _fieldWeights = fieldWeights;
        _listSizeMetadata = listSizeMetadata;
        _argumentWeights = argumentWeights;
        _inputFieldWeights = inputFieldWeights;
        _directiveArgumentWeights = directiveArgumentWeights;
    }

    /// <summary>
    /// Gets the options this snapshot was built with.
    /// </summary>
    public CostEngineOptions Options { get; }

    /// <summary>
    /// Builds a snapshot of <paramref name="schema"/>'s cost-relevant
    /// metadata.
    /// </summary>
    /// <param name="schema">
    /// The schema to snapshot.
    /// </param>
    /// <param name="options">
    /// The engine options that apply to <paramref name="schema"/>.
    /// </param>
    /// <returns>
    /// The immutable snapshot.
    /// </returns>
    public static CostSchemaSnapshot Create(ISchemaDefinition schema, CostEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(options);

        return CostSchemaSnapshotBuilder.Build(schema, options);
    }

    /// <summary>
    /// Gets the object type's dense index, used to address its bit in a
    /// <see cref="PossibleTypeSet"/>.
    /// </summary>
    internal int GetObjectTypeIndex(string objectTypeName) => _objectTypeIndex[objectTypeName];

    /// <summary>
    /// Gets the possible object types of an object, interface or union type,
    /// as a bitset over the snapshot's dense object-type index.
    /// </summary>
    internal PossibleTypeSet GetPossibleTypeSet(string typeName) => _possibleTypes[typeName];

    /// <summary>
    /// Gets a named type's own weight (<c>returnTypeWeight</c>): an object,
    /// scalar or enum type's explicit <c>@cost</c> usage or kind default, or
    /// an interface's or union's signed max over its member object types.
    /// </summary>
    internal double GetTypeWeight(string typeName) => _typeWeights[typeName];

    /// <summary>
    /// Gets an output field's own weight.
    /// </summary>
    internal double GetFieldWeight(string typeName, string fieldName)
        => _fieldWeights[new FieldKey(typeName, fieldName)];

    /// <summary>
    /// Gets an output field's <c>@listSize</c> metadata, or
    /// <see langword="null"/> when the field carries no <c>@listSize</c>
    /// usage.
    /// </summary>
    internal ListSizeMetadata? GetListSizeMetadata(string typeName, string fieldName)
        => _listSizeMetadata.TryGetValue(new FieldKey(typeName, fieldName), out var metadata)
            ? metadata
            : null;

    /// <summary>
    /// Gets an output field argument's own weight.
    /// </summary>
    internal double GetArgumentWeight(string typeName, string fieldName, string argumentName)
        => _argumentWeights[new ArgumentKey(typeName, fieldName, argumentName)];

    /// <summary>
    /// Gets an input object field's own weight.
    /// </summary>
    internal double GetInputFieldWeight(string inputTypeName, string fieldName)
        => _inputFieldWeights[new FieldKey(inputTypeName, fieldName)];

    /// <summary>
    /// Gets a directive definition argument's own weight, or 0.0 when the
    /// directive or the argument is not defined in this schema.
    /// </summary>
    internal double GetDirectiveArgumentWeight(string directiveName, string argumentName)
        => _directiveArgumentWeights.TryGetValue(
            new DirectiveArgumentKey(directiveName, argumentName),
            out var weight)
            ? weight
            : 0.0;
}
