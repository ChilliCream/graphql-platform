namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.C;

/// <summary>
/// The <c>Media</c> stand-in object in the <c>c</c> subgraph
/// (<c>type Media @key(fields: "id") @interfaceObject</c>).
/// </summary>
public sealed class Media
{
    public string Id { get; init; } = default!;
}
