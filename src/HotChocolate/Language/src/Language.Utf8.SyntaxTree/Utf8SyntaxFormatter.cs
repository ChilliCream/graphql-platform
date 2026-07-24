using System.Buffers;

namespace HotChocolate.Language;

/// <summary>
/// Writes verbatim source ranges of a <see cref="Utf8OperationDocument"/> while splicing in
/// variable name replacements driven by the position-sorted variable-site table.
/// </summary>
internal static class Utf8SyntaxFormatter
{
    /// <summary>
    /// Writes the source range of the node at <paramref name="cursor"/>, substituting variable
    /// names through <paramref name="variables"/>.
    /// </summary>
    internal static void Write(
        Utf8OperationDocument document,
        int cursor,
        IBufferWriter<byte> writer,
        Utf8VariableNameMap variables)
    {
        var row = document.GetRow(cursor);
        WriteRange(document, row.Location, row.SourceEnd, writer, variables);
    }

    /// <summary>
    /// Writes the source range <c>[start, end)</c>, substituting the name token of every variable
    /// site inside the range through <paramref name="variables"/>.
    /// </summary>
    internal static void WriteRange(
        Utf8OperationDocument document,
        int start,
        int end,
        IBufferWriter<byte> writer,
        Utf8VariableNameMap variables)
    {
        var siteCount = document.VariableSiteCount;
        if (variables.IsEmpty || siteCount == 0)
        {
            writer.Write(document.GetSource(start, end - start));
            return;
        }

        var copyStart = start;
        var index = document.FindFirstVariableSite(start);

        while (index < siteCount)
        {
            var position = document.GetVariableSitePosition(index);
            if (position >= end)
            {
                break;
            }

            var ordinal = document.GetVariableSiteOrdinal(index);
            if (variables.TryGetReplacement(ordinal, out var name))
            {
                writer.Write(document.GetSource(copyStart, position - copyStart));
                writer.Write(name.Span);
                copyStart = position + document.GetVariableName(ordinal).Length;
            }

            index++;
        }

        writer.Write(document.GetSource(copyStart, end - copyStart));
    }
}
