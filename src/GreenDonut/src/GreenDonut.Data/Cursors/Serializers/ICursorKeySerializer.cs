namespace GreenDonut.Data.Cursors.Serializers;

public interface ICursorKeySerializer
{
    bool IsSupported(Type type);

    bool IsNullable(Type type);

    CursorKeyCompareMethod GetCompareToMethod(Type type);

    object Parse(ReadOnlySpan<byte> formattedKey);

    bool TryFormat(object key, Span<byte> buffer, out int written);
}
