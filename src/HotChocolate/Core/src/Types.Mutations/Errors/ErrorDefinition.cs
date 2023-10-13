namespace HotChocolate.Types;

internal sealed class ErrorDefinition(
    Type runtimeType,
    Type schemaType,
    CreateError? factory = null)
{
    private static readonly CreateError _empty = o => o;

    public Type SchemaType { get; } = schemaType;

    public Type RuntimeType { get; } = runtimeType;

    public CreateError Factory { get; } = factory ?? _empty;
}