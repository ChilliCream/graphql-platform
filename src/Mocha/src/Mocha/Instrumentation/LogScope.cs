using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Mocha;

/// <summary>
/// Logging scope state made of a fixed list of named values. Its text form lists the values as
/// <c>Key:Value</c> pairs separated by spaces.
/// </summary>
internal abstract class LogScope : IReadOnlyList<KeyValuePair<string, object?>>
{
    private string? _text;

    public abstract int Count { get; }

    public abstract KeyValuePair<string, object?> this[int index] { get; }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => _text ??= Format();

    private string Format()
    {
        var text = new DefaultInterpolatedStringHandler(0, 0, CultureInfo.InvariantCulture);

        for (var i = 0; i < Count; i++)
        {
            var (key, value) = this[i];

            if (i > 0)
            {
                text.AppendLiteral(" ");
            }

            text.AppendLiteral(key);
            text.AppendLiteral(":");
            text.AppendFormatted(value);
        }

        return text.ToStringAndClear();
    }
}
