using System;
using HotChocolate.Types;

namespace StrawberryShake.Generators
{
    public class TypeInfo
    {
        public string ClrTypeName { get; set; }

        public string SchemaTypeName { get; set; }

        public Type SerializationType { get; set; }

        public int ListLevel { get; set; }

        public bool IsNullable { get; set; }

        public IType Type { get; set; }
    }
}
