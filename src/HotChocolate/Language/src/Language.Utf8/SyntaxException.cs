using System;
using System.Text;

namespace HotChocolate.Language;

[Serializable]
public class SyntaxException : Exception
{
    private const int _sourceTextRangeSize = 512;

    internal SyntaxException(Utf8GraphQLReader reader, string message) : base(message)
    {
        Position = reader.Position;
        Line = reader.Line;
        Column = reader.Column;
        SourceTextOffset = CalculateSourceTextOffset(reader);
        SourceText = SliceSourceText(reader, SourceTextOffset);
    }

    internal SyntaxException(Utf8GraphQLReader reader, string message, params object[] args) : this(
        reader,
        string.Format(message, args))
    {
    }

    public int Position { get; }

    public int Line { get; }

    public int Column { get; }

    public string SourceText { get; }

    public int SourceTextOffset { get; set; }

    private static int CalculateSourceTextOffset(Utf8GraphQLReader reader)
    {
        if (reader.GraphQLData.Length <= _sourceTextRangeSize ||
            reader.Position <= _sourceTextRangeSize / 2)
        {
            return 0;
        }

        if (reader.GraphQLData.Length - reader.Position <= _sourceTextRangeSize / 2)
        {
            return reader.GraphQLData.Length - _sourceTextRangeSize;
        }

        return reader.Position - _sourceTextRangeSize / 2;
    }

    private static string SliceSourceText(Utf8GraphQLReader reader, int offset)
    {
        var offsetLength = reader.GraphQLData.Length - offset;
        if (offsetLength > _sourceTextRangeSize)
        {
            offsetLength = _sourceTextRangeSize;
        }

        var slice = reader.GraphQLData
            .Slice(offset, offsetLength)
            .ToArray();

        return Encoding.UTF8.GetString(slice);
    }
}
