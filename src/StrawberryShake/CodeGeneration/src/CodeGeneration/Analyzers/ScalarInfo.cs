using HotChocolate;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Analyzers;

public readonly struct LeafTypeInfo
{
    public LeafTypeInfo(
        string typeName,
        string? runtimeTypeType = null,
        string? serializationType = null)
    {
        TypeName = typeName.EnsureGraphQLName();
        RuntimeType = runtimeTypeType ?? TypeNames.String;
        SerializationType = serializationType ?? TypeNames.String;
    }

    public string TypeName { get; }

    public string RuntimeType { get; }

    public string SerializationType { get; }
}
