namespace HotChocolate.Types;

internal sealed class ErrorDefinition
{
    public ErrorDefinition(Type runtimeType, Type schemaType, CreateError factory)
    {
        RuntimeType = runtimeType;
        SchemaType = schemaType;
        Factory = factory;
    }

    public Type SchemaType { get; }

    public Type RuntimeType { get; }

    public CreateError Factory { get; }
}
