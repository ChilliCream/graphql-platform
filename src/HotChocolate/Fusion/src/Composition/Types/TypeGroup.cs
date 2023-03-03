namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents a group of types to be merged into a single type.
/// </summary>
/// <param name="Name">
/// Gets the name of the merged type.
/// </param>
/// <param name="Parts">
/// Gets the parts that make up the merged type.
/// </param>
internal sealed record TypeGroup(
    string Name,
    IReadOnlyList<TypePart> Parts);
