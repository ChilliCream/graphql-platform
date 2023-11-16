using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers;

public readonly struct LeafTypeInfo(
    string typeName,
    string? runtimeTypeType = null,
    string? serializationType = null)
{
    public string TypeName { get; } = typeName.EnsureGraphQLName();

    public string RuntimeType { get; } = runtimeTypeType ?? TypeNames.String;

    public string SerializationType { get; } = serializationType ?? TypeNames.String;
}
