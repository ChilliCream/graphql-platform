using System;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public readonly struct ScalarInfo
    {
        public ScalarInfo(NameString typeName, string serializationType, string runtimeTypeType)
        {
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            SerializationType = serializationType ?? 
                throw new ArgumentNullException(nameof(serializationType));
            RuntimeTypeType = runtimeTypeType ?? 
                throw new ArgumentNullException(nameof(runtimeTypeType));
        }

        public NameString TypeName { get; }

        public string SerializationType { get; }

        public string RuntimeTypeType { get; }
    }
}
