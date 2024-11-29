#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Relay;

internal sealed class StringNodeIdValueSerializer : INodeIdValueSerializer
{
    private readonly Encoding _utf8 = Encoding.UTF8;

    public bool IsSupported(Type type) => type == typeof(string);

    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is string s)
        {
            var requiredCapacity = _utf8.GetByteCount(s);
            if (buffer.Length < requiredCapacity)
            {
                written = 0;
                return NodeIdFormatterResult.BufferTooSmall;
            }

            Utf8GraphQLParser.ConvertToBytes(s, ref buffer);
            written = buffer.Length;
            return NodeIdFormatterResult.Success;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public unsafe bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        if (buffer.Length == 0)
        {
            value = string.Empty;
            return true;
        }

        fixed (byte* b = buffer)
        {
            value = _utf8.GetString(b, buffer.Length);
            return true;
        }
    }
}
