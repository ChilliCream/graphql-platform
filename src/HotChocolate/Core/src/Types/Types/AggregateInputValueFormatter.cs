using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace HotChocolate.Types
{
    internal sealed class AggregateInputValueFormatter : IInputValueFormatter
    {
        private readonly IReadOnlyList<IInputValueFormatter> _formatters;

        public AggregateInputValueFormatter(IList<IInputValueFormatter> formatters)
        {
            _formatters = formatters.ToArray()
                ?? throw new ArgumentNullException(nameof(formatters));
        }

        public object? OnAfterDeserialize(object? runtimeValue)
        {
            object? current = runtimeValue;

            for (int i = 0; i < _formatters.Count; i++)
            {
                current = _formatters[i].OnAfterDeserialize(current);
            }

            return current;
        }
    }
}
