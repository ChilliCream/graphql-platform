using HotChocolate.Fusion.Language;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// An argument of a field whose <c>@require</c> map reads a context.
/// </summary>
/// <param name="ArgumentName">The name of the argument.</param>
/// <param name="IsNonNull">Whether the type of the argument is non-null.</param>
/// <param name="Map">The text of the map.</param>
/// <param name="Value">The parsed map.</param>
internal sealed record ContextRequirementArgument(
    string ArgumentName,
    bool IsNonNull,
    string Map,
    IValueSelectionNode Value);
