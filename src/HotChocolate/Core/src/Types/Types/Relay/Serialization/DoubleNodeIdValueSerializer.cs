#nullable enable
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Relay;

internal sealed class DoubleNodeIdValueSerializer : INodeIdValueSerializer
{
    public bool IsSupported(Type type) => type == typeof(double) || type == typeof(double?);

    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is double i)
        {
            return Utf8Formatter.TryFormat(i, buffer, out written)
                ? NodeIdFormatterResult.Success
                : NodeIdFormatterResult.BufferTooSmall;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        if (Utf8Parser.TryParse(buffer, out double parsedValue, out _))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }
}
