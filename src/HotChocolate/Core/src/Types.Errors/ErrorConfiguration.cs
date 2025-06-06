namespace HotChocolate.Types;

internal sealed class ErrorConfiguration(
    Type runtimeType,
    Type schemaType,
    CreateError? factory = null)
{
    private static readonly CreateError s_empty = _ => null;

    public Type SchemaType { get; } = schemaType;

    public Type RuntimeType { get; } = runtimeType;

    public CreateError Factory { get; } = factory ?? s_empty;
}
