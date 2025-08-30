namespace HotChocolate.Fusion.Language;

internal static class FieldSelectionMapSyntaxPrinter
{
    private static readonly FieldSelectionMapSyntaxSerializer s_serializer
        = new(new SyntaxSerializerOptions { Indented = true });

    private static readonly FieldSelectionMapSyntaxSerializer s_serializerNoIdent
        = new(new SyntaxSerializerOptions { Indented = false });

    /// <summary>
    /// Prints the string representation of a <c>FieldSelectionMap</c> syntax node.
    /// </summary>
    /// <param name="node">The syntax node that shall be printed.</param>
    /// <param name="indented">Specifies if the printed string shall have indentations.</param>
    /// <param name="options">Specifies the options used when writing syntax.</param>
    /// <returns>
    /// Returns the printed <c>FieldSelectionMap</c> syntax tree.
    /// </returns>
    public static string Print(
        this IFieldSelectionMapSyntaxNode node,
        bool indented = true,
        StringSyntaxWriterOptions? options = null)
    {
        var writer = new StringSyntaxWriter(options);

        if (indented)
        {
            s_serializer.Serialize(node, writer);
        }
        else
        {
            s_serializerNoIdent.Serialize(node, writer);
        }

        return writer.ToString();
    }
}
