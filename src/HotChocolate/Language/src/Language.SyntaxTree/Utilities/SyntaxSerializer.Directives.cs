namespace HotChocolate.Language.Utilities;

public sealed partial class SyntaxSerializer
{
    private void WriteDirective(DirectiveNode node, ISyntaxWriter writer)
    {
        if (!_indented || node.Arguments.Count == 0)
        {
            writer.WriteDirective(node);
            return;
        }

        var flatWidth = MeasureFlatDirective(node);

        if (!ArgumentsContainBlockString(node.Arguments)
            && writer.Column + flatWidth <= _printWidth)
        {
            writer.WriteDirective(node);
            return;
        }

        writer.Write('@');
        writer.WriteName(node.Name);
        writer.Write('(');
        writer.Indent();

        foreach (var argument in node.Arguments)
        {
            writer.WriteLine();
            writer.WriteIndent();
            WriteArgument(argument, writer);
        }

        writer.WriteLine();
        writer.Unindent();
        writer.WriteIndent();
        writer.Write(')');
    }

    private int MeasureFlatDirective(DirectiveNode node)
    {
        var writer = StringSyntaxWriter.Rent();

        try
        {
            writer.WriteDirective(node);
            return writer.Column;
        }
        finally
        {
            StringSyntaxWriter.Return(writer);
        }
    }
}
