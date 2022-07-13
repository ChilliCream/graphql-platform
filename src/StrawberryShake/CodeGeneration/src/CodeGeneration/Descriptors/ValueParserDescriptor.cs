namespace StrawberryShake.CodeGeneration.Descriptors;

public readonly struct ValueParserDescriptor
{
    public ValueParserDescriptor(
        string name,
        RuntimeTypeInfo runtimeType,
        RuntimeTypeInfo serializedType)
    {
        Name = name;
        RuntimeType = runtimeType;
        SerializedType = serializedType;
    }

    /// <summary>
    /// Gets the GraphQL type name.
    /// </summary>
    public string Name { get; }

    public RuntimeTypeInfo RuntimeType { get; }

    public RuntimeTypeInfo SerializedType { get; }
}
