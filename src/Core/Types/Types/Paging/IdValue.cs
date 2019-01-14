using System;

namespace HotChocolate.Types.Paging
{
    public readonly struct IdValue
    {
        public IdValue(NameString typeName, object value)
        {
            typeName.EnsureNotEmpty(typeName);
            TypeName = typeName;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public NameString TypeName { get; }

        public object Value { get; }
    }
}
