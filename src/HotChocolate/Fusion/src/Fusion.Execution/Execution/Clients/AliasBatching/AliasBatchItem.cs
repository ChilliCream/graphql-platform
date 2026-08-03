using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Represents one logical operation of a batched request: the root selection of a source schema
/// request paired with a single set of variable values.
/// </summary>
/// <param name="requestIndex">The index of the request this item was flattened from.</param>
/// <param name="rootField">The root selection of the request's operation.</param>
/// <param name="lookupTypeName">
/// The name of the type that the body of <paramref name="rootField"/> is selected on.
/// </param>
/// <param name="variables">The variable values of this item.</param>
internal readonly struct AliasBatchItem(
    int requestIndex,
    Utf8FieldNode rootField,
    string lookupTypeName,
    VariableValues variables)
{
    /// <summary>
    /// Gets the index of the request this item was flattened from.
    /// </summary>
    public int RequestIndex { get; } = requestIndex;

    /// <summary>
    /// Gets the root selection of the request's operation.
    /// </summary>
    public Utf8FieldNode RootField { get; } = rootField;

    /// <summary>
    /// Gets the name of the type that the body of <see cref="RootField"/> is selected on.
    /// </summary>
    public string LookupTypeName { get; } = lookupTypeName;

    /// <summary>
    /// Gets the variable values of this item.
    /// </summary>
    public VariableValues Variables { get; } = variables;
}
