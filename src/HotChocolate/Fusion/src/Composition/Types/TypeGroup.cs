namespace HotChocolate.Fusion.Composition.Types;

public sealed record TypeGroup(
    string Name,
    IReadOnlyList<TypePart> Parts);
