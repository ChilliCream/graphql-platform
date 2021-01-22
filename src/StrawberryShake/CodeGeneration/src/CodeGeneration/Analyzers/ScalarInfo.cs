using HotChocolate;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public readonly struct LeafTypeInfo
    {
        public LeafTypeInfo(
            NameString typeName,
            string? runtimeTypeType = null,
            string? serializationType = null)
        {
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            RuntimeType = runtimeTypeType ?? TypeNames.String;
            SerializationType = serializationType ?? TypeNames.String;
        }

        public NameString TypeName { get; }

        public string RuntimeType { get; }

        public string SerializationType { get; }
    }
}
