using System;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public interface ITypeInfo
    {
        string ClrTypeName { get; }

        string SchemaTypeName { get; }

        IType Type { get; }

        Type SerializationType { get; }

        int ListLevel { get; }

        bool IsNullable { get; }

        bool IsValueType { get; }
    }
}
