using System.Collections.Generic;
using System.Linq;

namespace Zeus.Abstractions
{
    public sealed class ListValue
        : IValue
    {
        public ListValue(IEnumerable<IValue> items)
        {
            Items = items.ToArray();
        }

        public IReadOnlyCollection<IValue> Items { get; }

        public override string ToString()
        {
            return "[" + string.Join(", ", Items.Select(t => t.ToString())) + "]";
        }
    }
}