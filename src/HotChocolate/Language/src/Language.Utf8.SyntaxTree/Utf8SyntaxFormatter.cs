using System.Buffers;

namespace HotChocolate.Language;

/// <summary>
/// Writes the GraphQL source text of a <see cref="Utf8OperationDocument"/> while splicing in
/// variable name replacements driven by the position-sorted variable-site table. A node is written
/// either verbatim, reproducing the formatting of the source document, or in canonical form, which
/// walks the packed metadata rows and emits every token the rows describe with the minimal
/// separators the grammar requires.
/// </summary>
internal static partial class Utf8SyntaxFormatter
{
    /// <summary>
    /// Writes the whole document as a complete document fragment, substituting variable names
    /// through <paramref name="variables"/>. When <paramref name="formatAsJsonStringValue"/> is
    /// <see langword="true"/> the output is written as a JSON string value, delimiters included.
    /// </summary>
    internal static void FormatDocument(
        Utf8OperationDocument document,
        IBufferWriter<byte> writer,
        bool indented,
        bool formatAsJsonStringValue,
        Utf8VariableNameMap variables)
    {
        var target = new Utf8SyntaxWriter(writer, formatAsJsonStringValue);
        target.WriteDelimiter();

        if (indented)
        {
            WriteVerbatimRange(document, 0, document.SourceLength, target, variables);
        }
        else
        {
            var walker = new RowWalker(document, target, variables, default, default);
            walker.WriteDocument();
        }

        target.WriteDelimiter();
    }

    /// <summary>
    /// Writes the node at <paramref name="cursor"/> as a complete document fragment, substituting
    /// variable names through <paramref name="variables"/>. When
    /// <paramref name="formatAsJsonStringValue"/> is <see langword="true"/> the output is written
    /// as a JSON string value, delimiters included.
    /// </summary>
    internal static void Format(
        Utf8OperationDocument document,
        int cursor,
        IBufferWriter<byte> writer,
        bool indented,
        bool formatAsJsonStringValue,
        Utf8VariableNameMap variables)
    {
        var target = new Utf8SyntaxWriter(writer, formatAsJsonStringValue);
        target.WriteDelimiter();
        Write(document, cursor, target, variables, indented);
        target.WriteDelimiter();
    }

    /// <summary>
    /// Writes the node at <paramref name="cursor"/>, substituting variable names through
    /// <paramref name="variables"/>. When <paramref name="indented"/> is <see langword="true"/>
    /// the node's source range is reproduced verbatim; otherwise the node is written in canonical
    /// form.
    /// </summary>
    internal static void Write(
        Utf8OperationDocument document,
        int cursor,
        Utf8SyntaxWriter writer,
        Utf8VariableNameMap variables,
        bool indented)
    {
        if (indented)
        {
            var row = document.GetRow(cursor);
            WriteVerbatimRange(document, row.Location, row.SourceEnd, writer, variables);
            return;
        }

        var walker = new RowWalker(document, writer, variables, default, default);
        walker.Write(cursor);
    }

    /// <summary>
    /// Writes the node at <paramref name="cursor"/> in canonical form, inserting
    /// <paramref name="variablePrefix"/> in front of the name of every variable whose ordinal is
    /// not in <paramref name="shared"/>. Shared variables keep their original name.
    /// </summary>
    internal static void Write(
        Utf8OperationDocument document,
        int cursor,
        Utf8SyntaxWriter writer,
        ReadOnlySpan<byte> variablePrefix,
        SharedOrdinalSet shared)
    {
        var walker = new RowWalker(document, writer, default, variablePrefix, shared);
        walker.Write(cursor);
    }

    private static void WriteVerbatimRange(
        Utf8OperationDocument document,
        int start,
        int end,
        Utf8SyntaxWriter writer,
        Utf8VariableNameMap variables)
    {
        var siteCount = document.VariableSiteCount;
        if (variables.IsEmpty || siteCount == 0)
        {
            writer.Write(document.GetSource(start, end - start));
            return;
        }

        var copyStart = start;
        for (var i = document.FindFirstVariableSite(start); i < siteCount; i++)
        {
            var position = document.GetVariableSitePosition(i);
            if (position >= end)
            {
                break;
            }

            var ordinal = document.GetVariableSiteOrdinal(i);
            if (variables.TryGetReplacement(ordinal, out var name))
            {
                writer.Write(document.GetSource(copyStart, position - copyStart));
                writer.Write(name.Span);
                copyStart = position + document.GetVariableName(ordinal).Length;
            }
        }

        writer.Write(document.GetSource(copyStart, end - copyStart));
    }
}
