using System;

namespace HotChocolate.Types.Relay
{
    public readonly struct IdValue
    {
        public IdValue(NameString schemaName, NameString typeName, object value)
        {
            SchemaName = schemaName;
            TypeName = typeName.EnsureNotEmpty(typeName); ;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public NameString SchemaName { get; }

        public NameString TypeName { get; }

        public object Value { get; }
    }
}
