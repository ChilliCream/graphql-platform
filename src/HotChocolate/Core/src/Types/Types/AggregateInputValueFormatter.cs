#nullable enable

namespace HotChocolate.Types;

internal sealed class AggregateInputValueFormatter : IInputValueFormatter
{
    private readonly IInputValueFormatter[] _formatters;

    public AggregateInputValueFormatter(IEnumerable<IInputValueFormatter> formatters)
    {
        if (formatters is null)
        {
            throw new ArgumentNullException(nameof(formatters));
        }

        if (formatters is IInputValueFormatter[] array)
        {
            _formatters = array;
        }
        else
        {
            _formatters = formatters.ToArray();
        }
    }

    public object? Format(object? originalValue)
    {
        var current = originalValue;

        foreach (var formatter in _formatters)
        {
            current = formatter.Format(current);
        }

        return current;
    }
}
