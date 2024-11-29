#nullable enable
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace HotChocolate.Types.Relay;

internal sealed class GuidNodeIdValueSerializer(bool compress = true) : INodeIdValueSerializer
{
    public bool IsSupported(Type type) => type == typeof(Guid) || type == typeof(Guid?);

    public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
    {
        if (value is Guid g)
        {
            if (compress)
            {
                if(buffer.Length < 16)
                {
                    written = 0;
                    return NodeIdFormatterResult.BufferTooSmall;
                }

                Span<byte> span = stackalloc byte[16];
#pragma warning disable CS9191
                MemoryMarshal.TryWrite(span, ref g);
#pragma warning restore CS9191
                span.CopyTo(buffer);
                written = 16;
                return NodeIdFormatterResult.Success;
            }

            return Utf8Formatter.TryFormat(g, buffer, out written, format: 'N')
                ? NodeIdFormatterResult.Success
                : NodeIdFormatterResult.BufferTooSmall;
        }

        written = 0;
        return NodeIdFormatterResult.InvalidValue;
    }

    public bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value)
    {
        if(compress && buffer.Length == 16)
        {
            value = new Guid(buffer);
            return true;
        }

        if (Utf8Parser.TryParse(buffer, out Guid parsedValue, out _, 'N'))
        {
            value = parsedValue;
            return true;
        }

        value = null;
        return false;
    }
}
