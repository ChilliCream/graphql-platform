using System;
using System.Collections.Generic;
using System.Linq;

namespace Zeus.Abstractions
{
    public sealed class InputObjectValue
        : IValue
    {
        public InputObjectValue(IReadOnlyDictionary<string, IValue> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Fields = fields;
        }

        public IReadOnlyDictionary<string, IValue> Fields { get; }

        public override string ToString()
        {
            return "{" + string.Join(", ", Fields.Select(t => t.Key + ": " + t.Value)) + "}";
        }

        object IValue.Value => Fields;
    }
}