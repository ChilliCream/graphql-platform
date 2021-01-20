using HotChocolate;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public readonly struct LeafTypeInfo
    {
        public LeafTypeInfo(
            NameString typeName,
            string? runtimeTypeType,
            string? serializationType)
        {
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            RuntimeTypeType = runtimeTypeType ?? TypeNames.SystemString;
            SerializationType = serializationType ?? TypeNames.SystemString;
        }

        public NameString TypeName { get; }

        public string RuntimeTypeType { get; }

        public string SerializationType { get; }
    }
}
