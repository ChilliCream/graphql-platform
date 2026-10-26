using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Caches the <see cref="FieldOptions"/> of the output fields of a merged schema.
/// </summary>
internal sealed class FieldOptionTable(MutableSchemaDefinition schema, SchemaSetTable schemaSets)
{
    private readonly Dictionary<MutableOutputFieldDefinition, FieldOptions> _options = [];
    private readonly Dictionary<(ITypeDefinition, string), MutableObjectTypeDefinition[]> _possibleTypes = [];

    /// <summary>
    /// Gets the options of <paramref name="field"/>.
    /// </summary>
    public FieldOptions GetOptions(MutableOutputFieldDefinition field)
    {
        if (!_options.TryGetValue(field, out var options))
        {
            options = CreateOptions(field);
            _options.Add(field, options);
        }

        return options;
    }

    /// <summary>
    /// Gets the object types that the field of <paramref name="options"/> returns in the schema of
    /// <paramref name="option"/>.
    /// </summary>
    public MutableObjectTypeDefinition[] GetPossibleTypes(FieldOptions options, FieldOption option)
        => option.PossibleTypes ??= GetPossibleTypes(options.FieldType, option.SchemaName);

    private FieldOptions CreateOptions(MutableOutputFieldDefinition field)
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
                field.GetFusionEventStreamMessage(schemaName));
        }

        return new FieldOptions(field, options);
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
