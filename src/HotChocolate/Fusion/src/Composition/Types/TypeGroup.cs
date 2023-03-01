namespace HotChocolate.Fusion.Composition;

public sealed record TypeGroup(
    string Name,
    IReadOnlyList<TypePart> Parts);
