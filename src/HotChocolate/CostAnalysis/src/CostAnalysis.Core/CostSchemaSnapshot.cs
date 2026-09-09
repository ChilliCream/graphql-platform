using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// An immutable, per-schema snapshot of a schema's cost-relevant metadata
/// that a <see cref="CostPlan"/> is compiled and evaluated against.
/// </summary>
public sealed class CostSchemaSnapshot
{
    private readonly FrozenDictionary<string, int> _objectTypeIndex;
    private readonly IComplexTypeDefinition[] _objectTypesByIndex;
    private readonly FrozenDictionary<string, PossibleTypeSet> _possibleTypes;
    private readonly FrozenDictionary<string, double> _typeWeights;
    private readonly FrozenDictionary<FieldKey, double> _fieldWeights;
    private readonly FrozenDictionary<FieldKey, ListSizeMetadata> _listSizeMetadata;
    private readonly FrozenDictionary<ArgumentKey, double> _argumentWeights;
    private readonly FrozenDictionary<FieldKey, double> _inputFieldWeights;
    private readonly FrozenDictionary<FieldKey, ImmutableArray<InputValueMetadata>> _fieldArguments;
    private readonly FrozenDictionary<string, ImmutableArray<InputValueMetadata>> _inputObjectFields;
    private readonly FrozenDictionary<string, ImmutableArray<DirectiveArgumentDefinition>> _directiveArguments;
    private readonly FrozenDictionary<string, ImmutableArray<InputValueMetadata>> _directiveArgumentMetadata;
    private readonly double _defaultListSize;
    private readonly int _caseBudget;

    internal CostSchemaSnapshot(
        double defaultListSize,
        int caseBudget,
        string? queryTypeName,
        string? mutationTypeName,
        string? subscriptionTypeName,
        FrozenDictionary<string, int> objectTypeIndex,
        IComplexTypeDefinition[] objectTypesByIndex,
        FrozenDictionary<string, PossibleTypeSet> possibleTypes,
        FrozenDictionary<string, double> typeWeights,
        FrozenDictionary<FieldKey, double> fieldWeights,
        FrozenDictionary<FieldKey, ListSizeMetadata> listSizeMetadata,
        FrozenDictionary<ArgumentKey, double> argumentWeights,
        FrozenDictionary<FieldKey, double> inputFieldWeights,
        FrozenDictionary<FieldKey, ImmutableArray<InputValueMetadata>> fieldArguments,
        FrozenDictionary<string, ImmutableArray<InputValueMetadata>> inputObjectFields,
        FrozenDictionary<string, ImmutableArray<DirectiveArgumentDefinition>> directiveArguments,
        FrozenDictionary<string, ImmutableArray<InputValueMetadata>> directiveArgumentMetadata)
    {
        _defaultListSize = defaultListSize;
        _caseBudget = caseBudget;
        QueryTypeName = queryTypeName;
        MutationTypeName = mutationTypeName;
        SubscriptionTypeName = subscriptionTypeName;
        _objectTypeIndex = objectTypeIndex;
        _objectTypesByIndex = objectTypesByIndex;
        _possibleTypes = possibleTypes;
        _typeWeights = typeWeights;
        _fieldWeights = fieldWeights;
        _listSizeMetadata = listSizeMetadata;
        _argumentWeights = argumentWeights;
        _inputFieldWeights = inputFieldWeights;
        _fieldArguments = fieldArguments;
        _inputObjectFields = inputObjectFields;
        _directiveArguments = directiveArguments;
        _directiveArgumentMetadata = directiveArgumentMetadata;
    }

    /// <summary>
    /// Gets a detached copy of the options this snapshot was built with.
    /// </summary>
    public CostEngineOptions Options
        => new()
        {
            DefaultListSize = _defaultListSize,
            CaseBudget = _caseBudget
        };

    internal double DefaultListSize => _defaultListSize;

    internal int CaseBudget => _caseBudget;

    internal string? QueryTypeName { get; }

    internal string? MutationTypeName { get; }

    internal string? SubscriptionTypeName { get; }

    internal string GetOperationTypeName(OperationType operation)
        => operation switch
        {
            OperationType.Query when QueryTypeName is { } name => name,
            OperationType.Mutation when MutationTypeName is { } name => name,
            OperationType.Subscription when SubscriptionTypeName is { } name => name,
            _ => throw ThrowHelper.OperationTypeNotDefined(operation)
        };

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
    /// Gets the number of object types in the schema's dense object-type
    /// index, the width every <see cref="PossibleTypeSet"/> is built over.
    /// </summary>
    internal int ObjectTypeCount => _objectTypesByIndex.Length;

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
    /// Gets the name of the one object type a single-member
    /// <see cref="PossibleTypeSet"/> contains.
    /// </summary>
    internal string GetSingletonObjectTypeName(PossibleTypeSet singleton)
    {
        var enumerator = singleton.GetEnumerator();
        enumerator.MoveNext();
        return _objectTypesByIndex[enumerator.Current].Name;
    }

    /// <summary>
    /// Gets the object type definition at the snapshot's dense
    /// <paramref name="objectTypeIndex"/>.
    /// </summary>
    internal IComplexTypeDefinition GetObjectTypeDefinition(int objectTypeIndex)
        => _objectTypesByIndex[objectTypeIndex];

    /// <summary>
    /// Gets the field definition <paramref name="fieldName"/> resolves to on
    /// the object type at the snapshot's dense <paramref name="objectTypeIndex"/>.
    /// </summary>
    internal IOutputFieldDefinition GetFieldDefinition(int objectTypeIndex, string fieldName)
        => _objectTypesByIndex[objectTypeIndex].Fields[fieldName];

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
    /// Gets an output field's arguments in declaration order.
    /// </summary>
    internal ImmutableArray<InputValueMetadata> GetFieldArguments(string typeName, string fieldName)
        => _fieldArguments[new FieldKey(typeName, fieldName)];

    /// <summary>
    /// Gets an input object's fields in declaration order.
    /// </summary>
    internal bool TryGetInputObjectFields(string typeName, out ImmutableArray<InputValueMetadata> fields)
        => _inputObjectFields.TryGetValue(typeName, out fields);

    /// <summary>
    /// Gets a directive definition's own arguments, in declaration order, or
    /// <see langword="false"/> when the directive is not defined in this
    /// schema.
    /// </summary>
    internal bool TryGetDirectiveArguments(string directiveName, out ImmutableArray<DirectiveArgumentDefinition> arguments)
        => _directiveArguments.TryGetValue(directiveName, out arguments);

    /// <summary>
    /// Gets a directive definition's arguments with their input metadata.
    /// </summary>
    internal bool TryGetDirectiveArgumentMetadata(string directiveName, out ImmutableArray<InputValueMetadata> arguments)
        => _directiveArgumentMetadata.TryGetValue(directiveName, out arguments);
}
