using System;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public class LeafValue
    {
        public LeafValue(ISerializableType type, object value)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
        }

        public ISerializableType Type { get; }

        public object Value { get; }
    }
}
