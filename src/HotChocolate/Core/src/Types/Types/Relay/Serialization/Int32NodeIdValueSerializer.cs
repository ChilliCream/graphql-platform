#nullable enable
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Relay;

internal sealed class Int32NodeIdValueSerializer : INodeIdValueSerializer
{
    public bool IsSupported(Type type) => type == typeof(int) || type == typeof(int?);

    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is int i)
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
        if (Utf8Parser.TryParse(buffer, out int parsedValue, out _))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }
}
