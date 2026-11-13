using System.Collections.Immutable;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Caches the <see cref="FieldOptionSet"/> of the output fields of a merged schema.
/// </summary>
internal sealed class FieldOptionCache(MutableSchemaDefinition schema, SchemaSetInterner schemaSets)
{
    private readonly Dictionary<MutableOutputFieldDefinition, FieldOptionSet> _options = [];
    private readonly Dictionary<(ITypeDefinition, string), MutableObjectTypeDefinition[]> _possibleTypes = [];
    private readonly Dictionary<(MutableOutputFieldDefinition, string), ImmutableArray<string>> _contextNames = [];
    private readonly Dictionary<(MutableOutputFieldDefinition, string), ImmutableDictionary<string, SelectionSetNode>>
        _contextSelections = [];

    /// <summary>
    /// Gets the options of <paramref name="field"/>.
    /// </summary>
    public FieldOptionSet GetOptions(MutableOutputFieldDefinition field)
    {
        if (!_options.TryGetValue(field, out var options))
        {
            options = CreateOptions(field);
            _options.Add(field, options);
        }

        return options;
    }

    /// <summary>
    /// Gets the names of the contexts that the requirements of <paramref name="field"/> read in
    /// <paramref name="schemaName"/>, which is empty if it reads none.
    /// </summary>
    public ImmutableArray<string> GetContextNames(MutableOutputFieldDefinition field, string schemaName)
    {
        if (!_contextNames.TryGetValue((field, schemaName), out var contextNames))
        {
            contextNames = field.GetFusionRequiresContextNames(schemaName);
            _contextNames.Add((field, schemaName), contextNames);
        }

        return contextNames;
    }

    /// <summary>
    /// Gets the selections that the requirements of <paramref name="field"/> read from each context in
    /// <paramref name="schemaName"/>, keyed by context name, which is empty if it reads none.
    /// </summary>
    public ImmutableDictionary<string, SelectionSetNode> GetContextSelections(
        MutableOutputFieldDefinition field,
        string schemaName)
    {
        if (!_contextSelections.TryGetValue((field, schemaName), out var contextSelections))
        {
            contextSelections = field.GetFusionRequiresContextSelections(schemaName);
            _contextSelections.Add((field, schemaName), contextSelections);
        }

        return contextSelections;
    }

    /// <summary>
    /// Determines whether <paramref name="field"/> has requirements in <paramref name="schemaName"/>,
    /// either on its declaring type or on a context.
    /// </summary>
    public bool HasRequirements(MutableOutputFieldDefinition field, string schemaName)
        => field.GetFusionRequiresRequirements(schemaName) is not null
            || !GetContextNames(field, schemaName).IsEmpty;

    /// <summary>
    /// Gets the object types that the field of <paramref name="options"/> returns in the schema of
    /// <paramref name="option"/>.
    /// </summary>
    public MutableObjectTypeDefinition[] GetPossibleTypes(FieldOptionSet options, FieldOption option)
        => option.PossibleTypes ??= GetPossibleTypes(options.FieldType, option.SchemaName);

    private FieldOptionSet CreateOptions(MutableOutputFieldDefinition field)
    {
        // The schemas of an event stream replace the schemas of the field as its options.
        var schemaNames = field.GetFusionEventStreamSchemaNames();

        if (schemaNames.IsDefaultOrEmpty)
        {
            schemaNames = field.GetSchemaNames();
        }

        var options = new FieldOption[schemaNames.Length];

        for (var i = 0; i < schemaNames.Length; i++)
        {
            var schemaName = schemaNames[i];

            options[i] = new FieldOption(
                schemaName,
                schemaSets.GetIndex(schemaName),
                field.IsPartial(schemaName),
                field.GetFusionRequiresRequirements(schemaName),
                GetContextNames(field, schemaName),
                field.GetFusionEventStreamMessage(schemaName));
        }

        return new FieldOptionSet(field, options);
    }

    private MutableObjectTypeDefinition[] GetPossibleTypes(ITypeDefinition type, string schemaName)
    {
        if (!_possibleTypes.TryGetValue((type, schemaName), out var possibleTypes))
        {
            possibleTypes = type is MutableObjectTypeDefinition objectType
                ? [objectType]
                : [.. type.GetPossibleTypes(schemaName, schema)];
            _possibleTypes.Add((type, schemaName), possibleTypes);
        }

        return possibleTypes;
    }
}
