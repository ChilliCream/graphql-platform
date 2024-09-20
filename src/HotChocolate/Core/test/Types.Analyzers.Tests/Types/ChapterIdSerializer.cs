using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

public sealed class ChapterIdSerializer : CompositeNodeIdValueSerializer<ChapterId>
{
    protected override NodeIdFormatterResult Format(Span<byte> buffer, ChapterId value, out int written)
    {
        if (TryFormatIdPart(buffer, value.BookId, out var bookIdLength) &&
            TryFormatIdPart(buffer.Slice(bookIdLength), value.Number, out var numberLength))
        {
            written = bookIdLength + numberLength;
            return NodeIdFormatterResult.Success;
        }

        written = 0;
        return NodeIdFormatterResult.BufferTooSmall;
    }

    protected override bool TryParse(ReadOnlySpan<byte> buffer, out ChapterId value)
    {
        if (TryParseIdPart(buffer, out int bookId, out var consumed) &&
            TryParseIdPart(buffer.Slice(consumed), out int chapterNumber, out _))
        {
            value = new ChapterId(chapterNumber, bookId);
            return true;
        }

        value = default;
        return false;
    }
}
