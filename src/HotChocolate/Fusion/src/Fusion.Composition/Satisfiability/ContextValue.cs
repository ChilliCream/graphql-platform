using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// The nearest value above a position of a query path whose type sets a context.
/// </summary>
/// <param name="ObjectType">The object type of the value.</param>
/// <param name="SchemaName">
/// The schema that holds the value, or <c>null</c> if the value is an operation root.
/// </param>
/// <param name="Key">
/// The visit key of the value, or <c>null</c> if the scope was created from an access path.
/// </param>
internal sealed record ContextValue(
    MutableObjectTypeDefinition ObjectType,
    string? SchemaName,
    VisitKey? Key);
