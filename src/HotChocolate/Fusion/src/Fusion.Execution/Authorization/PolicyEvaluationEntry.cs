using HotChocolate.Execution;

namespace HotChocolate.Fusion.Authorization;

/// <summary>
/// One occurrence of a policy within a variable set, with the coerced arguments of the selection.
/// </summary>
/// <param name="Descriptor">
/// The descriptor of the occurrence.
/// </param>
/// <param name="Arguments">
/// The coerced arguments of the selection for the variable set.
/// </param>
public readonly record struct PolicyEvaluationEntry(
    PolicyDescriptor Descriptor,
    IReadOnlyDictionary<string, object?> Arguments)
{
    /// <summary>
    /// Gets the selection the occurrence applies to.
    /// </summary>
    public ISelection Selection => Descriptor.Selection;

    internal int Slot { get; init; }
}
