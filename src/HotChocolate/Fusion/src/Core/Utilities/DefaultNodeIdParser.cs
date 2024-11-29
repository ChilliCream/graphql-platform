using System.Buffers;
using System.Text;
using HotChocolate.Types.Relay;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Utilities;

internal sealed class DefaultNodeIdParser : NodeIdParser
{
    private static readonly SearchValues<byte> _delimiterSearchValues =
        SearchValues.Create([(byte)':', (byte)'\n']);
    private readonly Encoding _utf8 = Encoding.UTF8;
    private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public override string ParseTypeName(string id)
    {
        var size = id.Length;
        byte[]? buffer = null;
        var span = size <= 256 ? stackalloc byte[size] : buffer = _arrayPool.Rent(size);

        if (!Convert.TryFromBase64String(id, span, out var written))
        {
            throw new IdSerializationException(
                DefaultIdParser_ParseTypeName_InvalidFormat,
                OperationStatus.InvalidData,
                id);
        }

        var index = span[..written].IndexOfAny(_delimiterSearchValues);
        var typeName = span[..index];
        var s = _utf8.GetString(typeName);

        if (buffer is not null)
        {
            _arrayPool.Return(buffer);
        }

        return s;
    }
}
