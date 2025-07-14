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
        SelectedListValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write(LeftSquareBracket);

        if (node.SelectedValue is not null)
        {
            Visit(node.SelectedValue, writer);
        }
        else if (node.SelectedListValue is not null)
        {
            Visit(node.SelectedListValue, writer);
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
        SelectedObjectValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write(LeftBrace);
        WriteLineOrSpace(writer);

        writer.Indent();

        if (node.Fields.Count > 0)
        {
            Visit(node.Fields[0], writer);

            for (var i = 1; i < node.Fields.Count; i++)
            {
                WriteLineOrCommaSpace(writer);
                Visit(node.Fields[i], writer);
            }
        }

        writer.Unindent();

        WriteLineOrSpace(writer);
        writer.WriteIndent(options.Indented);
        writer.Write(RightBrace);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectedValueNode node,
        ISyntaxWriter writer)
    {
        Visit(node.SelectedValueEntry, writer);

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
