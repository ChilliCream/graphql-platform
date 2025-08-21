using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HotChocolate.Fusion.Execution;

internal sealed class DefaultNodeIdParser : INodeIdParser
{
    private static readonly SearchValues<byte> s_delimiterSearchValues =
        SearchValues.Create(":\n"u8);
    private readonly Encoding _utf8 = Encoding.UTF8;
    private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    public bool TryParseTypeNameFromId(string id, [NotNullWhen(true)] out string? typeName)
    {
        var size = id.Length;
        byte[]? buffer = null;
        var span = size <= 256 ? stackalloc byte[size] : buffer = _arrayPool.Rent(size);

        if (!Convert.TryFromBase64String(id, span, out var written))
        {
            typeName = null;
            return false;
        }

        var index = span[..written].IndexOfAny(s_delimiterSearchValues);

        if (index < 0 || index >= written - 1)
        {
            typeName = null;
            return false;
        }

        var typeNameSpan = span[..index];
        typeName = _utf8.GetString(typeNameSpan);

        if (buffer is not null)
        {
            _arrayPool.Return(buffer);
        }

        return true;
    }
}
