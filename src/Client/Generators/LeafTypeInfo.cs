using System;

namespace StrawberryShake.Generators
{
    public class LeafTypeInfo
    {
        public LeafTypeInfo(string typeName, Type clrType)
            : this(typeName, clrType, clrType)
        {
        }

        public LeafTypeInfo(
            string typeName,
            Type clrType,
            Type serializationType)
        {
            TypeName = typeName;
            ClrType = clrType;
            SerializationType = serializationType;
        }

        public string TypeName { get; }

        public Type ClrType { get; }

        public Type SerializationType { get; }
    }
}
