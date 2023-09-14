using System.Buffers;
using System.Text;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Utilities;

internal sealed class DefaultIdParser : IdParser
{
    private readonly byte[] _separators = ":\n"u8.ToArray();
    private readonly Encoding _utf8 = Encoding.UTF8;

    public override string ParseTypeName(string id)
    {
        var size = id.Length;
        byte[]? buffer = null;
        var span = size <= 256
            ? stackalloc byte[size]
            : buffer = ArrayPool<byte>.Shared.Rent(size);

        if (Convert.TryFromBase64String(id, span, out var written))
        {
            var index = span[..written].IndexOfAny(_separators);
            var typeName = span[..index];
            var s = _utf8.GetString(typeName);

            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return s;
        }

        throw new IdSerializationException("Unable to decode the node id value.", OperationStatus.Done, id);
    }
}