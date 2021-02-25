using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types
{
    internal sealed class AggregateInputValueFormatter : IInputValueFormatter
    {
        private readonly IList<IInputValueFormatter> _formatters;

        public AggregateInputValueFormatter(IList<IInputValueFormatter> formatters)
        {
            _formatters = formatters ?? throw new ArgumentNullException(nameof(formatters));
        }

        public object? OnAfterDeserialize(object? runtimeValue)
        {
            object? current = runtimeValue;

            for (var i = 0; i < _formatters.Count; i++)
            {
                current = _formatters[i].OnAfterDeserialize(current);
            }

            return current;
        }
    }
}
