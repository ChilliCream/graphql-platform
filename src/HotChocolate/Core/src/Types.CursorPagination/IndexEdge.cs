using System.Buffers.Text;

namespace HotChocolate.Types.Pagination;

public sealed class IndexEdge<T> : Edge<T>
{
    private IndexEdge(T node, string cursor, int index)
        : base(node, cursor)
    {
        Index = index;
    }

    public int Index { get; }

    public static IndexEdge<T> Create(T node, int index)
    {
        Span<byte> buffer = stackalloc byte[27 / 3 * 4];
        Utf8Formatter.TryFormat(index, buffer, out var written);
        Base64.EncodeToUtf8InPlace(buffer, written, out written);
        var cursor = IndexCursor.Format(buffer.Slice(0, written));
        return new IndexEdge<T>(node, cursor, index);
    }
}
