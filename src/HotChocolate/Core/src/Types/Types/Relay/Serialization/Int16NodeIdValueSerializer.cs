#nullable enable
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Relay;

internal sealed class Int16NodeIdValueSerializer : INodeIdValueSerializer
{
    public bool IsSupported(Type type) => type == typeof(short) || type == typeof(short?);

    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is short i)
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
        if (Utf8Parser.TryParse(buffer, out short parsedValue, out _))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }
}
