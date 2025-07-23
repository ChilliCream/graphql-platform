using static HotChocolate.Fusion.Language.CharConstants;

namespace HotChocolate.Fusion.Language;

internal class FieldSelectionMapSyntaxSerializer(SyntaxSerializerOptions options)
    : FieldSelectionMapSyntaxVisitor<ISyntaxWriter>
{
    public void Serialize(IFieldSelectionMapSyntaxNode node, ISyntaxWriter writer)
    {
        Visit(node, writer);
    }

    protected override ISyntaxVisitorAction Enter(
        NameNode node,
        ISyntaxWriter writer)
    {
        writer.Write(node.Value);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        PathNode node,
        ISyntaxWriter writer)
    {
        if (node.TypeName is not null)
        {
            writer.Write(LeftAngleBracket);
            writer.Write(node.TypeName.Value);
            writer.Write(RightAngleBracket);
            writer.Write(Period);
        }

        Visit(node.PathSegment, writer);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        PathSegmentNode node,
        ISyntaxWriter writer)
    {
        writer.Write(node.FieldName.Value);

        if (node.TypeName is not null)
        {
            writer.Write(LeftAngleBracket);
            writer.Write(node.TypeName.Value);
            writer.Write(RightAngleBracket);
        }

        if (node.PathSegment is not null)
        {
            writer.Write(Period);
            Visit(node.PathSegment, writer);
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ListValueSelectionNode selectionNode,
        ISyntaxWriter writer)
    {
        writer.Write(LeftSquareBracket);

        if (selectionNode.SelectedValue is not null)
        {
            Visit(selectionNode.SelectedValue, writer);
        }
        else if (selectionNode.ListValueSelection is not null)
        {
            Visit(selectionNode.ListValueSelection, writer);
        }

        writer.Write(RightSquareBracket);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectedObjectFieldNode node,
        ISyntaxWriter writer)
    {
        writer.WriteIndent(options.Indented);

        writer.Write(node.Name.Value);

        if (node.SelectedValue is not null)
        {
            writer.Write($"{Colon} ");
            Visit(node.SelectedValue, writer);
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectValueSelectionNode selectionNode,
        ISyntaxWriter writer)
    {
        writer.Write(LeftBrace);
        WriteLineOrSpace(writer);

        writer.Indent();

        if (selectionNode.Fields.Count > 0)
        {
            Visit(selectionNode.Fields[0], writer);

            for (var i = 1; i < selectionNode.Fields.Count; i++)
            {
                WriteLineOrCommaSpace(writer);
                Visit(selectionNode.Fields[i], writer);
            }
        }

        writer.Unindent();

        WriteLineOrSpace(writer);
        writer.WriteIndent(options.Indented);
        writer.Write(RightBrace);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ChoiceValueSelectionNode node,
        ISyntaxWriter writer)
    {
        Visit(node.Entries, writer);

        if (node.SelectedValue is not null)
        {
            writer.Write($" {Pipe} ");
            Visit(node.SelectedValue, writer);
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectedValueEntryNode node,
        ISyntaxWriter writer)
    {
        if (node.Path is not null)
        {
            Visit(node.Path, writer);

            if (node.SelectedObjectValue is not null)
            {
                writer.Write(Period);
            }
        }

        if (node.SelectedObjectValue is not null)
        {
            Visit(node.SelectedObjectValue, writer);
        }

        if (node.SelectedListValue is not null)
        {
            Visit(node.SelectedListValue, writer);
        }

        return Skip;
    }

    private void WriteLineOrSpace(ISyntaxWriter writer)
    {
        if (options.Indented)
        {
            writer.WriteLine();
        }
        else
        {
            writer.WriteSpace();
        }
    }

    private void WriteLineOrCommaSpace(ISyntaxWriter writer)
    {
        if (options.Indented)
        {
            writer.WriteLine();
        }
        else
        {
            writer.Write(", ");
        }
    }
}
