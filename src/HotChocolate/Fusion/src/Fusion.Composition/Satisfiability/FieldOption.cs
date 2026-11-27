using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// A source schema that is a candidate for resolving an output field, with the field's directive
/// data in that schema.
/// </summary>
internal sealed class FieldOption(
    string schemaName,
    int schemaIndex,
    bool isPartial,
    SelectionSetNode? requirements,
    ImmutableArray<string> contextNames,
    SelectionSetNode? eventStreamMessage)
{
    /// <summary>
    /// Gets the name of the schema.
    /// </summary>
    public string SchemaName { get; } = schemaName;

    /// <summary>
    /// Gets the index of the schema in the <see cref="SchemaSetInterner"/>.
    /// </summary>
    public int SchemaIndex { get; } = schemaIndex;

    /// <summary>
    /// Gets a value indicating whether the field is partial (<c>@external</c>) in the schema.
    /// </summary>
    public bool IsPartial { get; } = isPartial;

    /// <summary>
    /// Gets the requirements of the field in the schema, or <c>null</c> if it has none.
    /// </summary>
    public SelectionSetNode? Requirements { get; } = requirements;

    /// <summary>
    /// Gets the names of the contexts that the requirements of the field in the schema read, which is
    /// empty if it reads none.
    /// </summary>
    public ImmutableArray<string> ContextNames { get; } = contextNames;

    /// <summary>
    /// Gets the event stream message of the field in the schema, or <c>null</c> if it has none.
    /// </summary>
    public SelectionSetNode? EventStreamMessage { get; } = eventStreamMessage;

    /// <summary>
    /// Gets or sets the object types the field returns in the schema, or <c>null</c> until the
    /// <see cref="FieldOptionCache"/> resolves them.
    /// </summary>
    internal MutableObjectTypeDefinition[]? PossibleTypes { get; set; }
}
