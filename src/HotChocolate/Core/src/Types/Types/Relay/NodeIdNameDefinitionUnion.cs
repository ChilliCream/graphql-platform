namespace HotChocolate.Types.Relay;

/// <summary>
/// A discriminated union, containing either a literal or a type that defines
/// the name of the node identifier.
/// </summary>
internal record NodeIdNameDefinitionUnion(string? Literal, Type? Type)
{
    public static NodeIdNameDefinitionUnion? Create(string? literal) =>
        literal == null ? null : new NodeIdNameDefinitionUnion(literal, null);

    public static NodeIdNameDefinitionUnion Create<T>() =>
        new NodeIdNameDefinitionUnion(null, typeof(T));
}
