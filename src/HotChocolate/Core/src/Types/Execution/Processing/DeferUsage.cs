namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a usage of the <c>@defer</c> directive encountered during operation compilation.
/// Forms a parent chain to model nested defer scopes.
/// </summary>
/// <param name="Label">
/// The optional label from <c>@defer(label: "...")</c>, used to identify the deferred
/// payload in the incremental delivery response.
/// </param>
/// <param name="Parent">
/// The parent defer usage when this <c>@defer</c> is nested inside another deferred fragment,
/// or <c>null</c> if this is a top-level defer.
/// </param>
/// <param name="DeferConditionIndex">
/// The index into the <see cref="DeferConditionCollection"/> for the <c>if</c> condition
/// associated with this defer directive. This index maps to a bit position in the
/// runtime defer flags bitmask.
/// </param>
public sealed record DeferUsage(
    string? Label,
    DeferUsage? Parent,
    byte DeferConditionIndex);
