namespace HotChocolate.Language.Utilities;

public sealed partial class SyntaxSerializer
{
    /// <summary>
    /// Defines how a separated list breaks when it does not fit on a single line.
    /// </summary>
    private enum SeparatedListBreakStyle
    {
        /// <summary>
        /// Each item appears on its own indented line, prefixed with the broken
        /// separator (e.g. union members and directive locations using "| ").
        /// The first item is also prefixed. The list opens on a new line.
        /// </summary>
        Leading,

        /// <summary>
        /// The first item stays inline with the surrounding prelude. Subsequent
        /// items appear on their own indented line. The separator is appended
        /// after every non-last item (e.g. implements lists using " &amp;").
        /// </summary>
        Trailing
    }

    /// <summary>
    /// Writes a separated list of items, picking either a flat single-line layout
    /// or a broken multi-line layout to match Prettier's group/ifBreak semantics.
    /// </summary>
    /// <param name="items">The items to write.</param>
    /// <param name="flatSeparator">
    /// The separator written between items in flat form (e.g. " &amp; " or " | ").
    /// </param>
    /// <param name="brokenSeparator">
    /// The separator written in broken form. For <see cref="SeparatedListBreakStyle.Leading"/>
    /// it is written before each item (e.g. "| "). For <see cref="SeparatedListBreakStyle.Trailing"/>
    /// it is written after every non-last item (e.g. " &amp;").
    /// </param>
    /// <param name="breakStyle">
    /// Whether the broken layout uses a leading or a trailing separator.
    /// </param>
    /// <param name="flatLeading">
    /// Text written between the surrounding prelude and the first item in flat form.
    /// In broken form this is replaced by a newline plus indent.
    /// </param>
    /// <param name="writeItem">Writes a single item.</param>
    /// <param name="writer">The output writer.</param>
    private void WriteSeparatedList<T>(
        IReadOnlyList<T> items,
        string flatSeparator,
        string brokenSeparator,
        SeparatedListBreakStyle breakStyle,
        string flatLeading,
        Action<T, ISyntaxWriter> writeItem,
        ISyntaxWriter writer)
    {
        if (items.Count == 0)
        {
            return;
        }

        if (!_indented)
        {
            writer.Write(flatLeading);
            writer.WriteMany(items, writeItem, flatSeparator);
            return;
        }

        var flatWidth = flatLeading.Length
            + MeasureFlatSeparatedList(items, flatSeparator, writeItem);

        if (writer.Column + flatWidth <= _printWidth)
        {
            writer.Write(flatLeading);
            writer.WriteMany(items, writeItem, flatSeparator);
            return;
        }

        if (breakStyle == SeparatedListBreakStyle.Leading)
        {
            writer.WriteLine();
            writer.Indent();

            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    writer.WriteLine();
                }

                writer.WriteIndent();
                writer.Write(brokenSeparator);
                writeItem(items[i], writer);
            }

            writer.Unindent();
        }
        else
        {
            writer.Indent();

            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    writer.WriteIndent();
                }

                writeItem(items[i], writer);

                if (i < items.Count - 1)
                {
                    writer.Write(brokenSeparator);
                    writer.WriteLine();
                }
            }

            writer.Unindent();
        }
    }

    private int MeasureFlatSeparatedList<T>(
        IReadOnlyList<T> items,
        string flatSeparator,
        Action<T, ISyntaxWriter> writeItem)
    {
        var writer = StringSyntaxWriter.Rent();

        try
        {
            writer.WriteMany(items, writeItem, flatSeparator);
            return writer.Column;
        }
        finally
        {
            StringSyntaxWriter.Return(writer);
        }
    }
}
