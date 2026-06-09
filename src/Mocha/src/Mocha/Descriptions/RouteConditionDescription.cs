namespace Mocha;

/// <summary>
/// Describes the match condition that determines whether an inbound route selects its consumer for a
/// received message, for diagnostic and visualization purposes.
/// </summary>
/// <param name="Kind">The kind of condition, such as the message type rule, a header rule, or a composite.</param>
/// <param name="Detail">
/// A condition specific detail, such as a message type identity or a header key, or <c>null</c> if not applicable.
/// </param>
/// <param name="Children">The nested conditions of a composite condition, or an empty list for a leaf condition.</param>
public sealed record RouteConditionDescription(
    string Kind,
    string? Detail,
    IReadOnlyList<RouteConditionDescription> Children);
