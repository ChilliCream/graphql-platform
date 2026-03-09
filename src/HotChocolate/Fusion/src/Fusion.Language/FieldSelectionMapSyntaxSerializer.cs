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
        PathListValueSelectionNode node,
        ISyntaxWriter writer)
    {
        Visit(node.Path, writer);
        Visit(node.ListValueSelection, writer);
        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ListValueSelectionNode node,
        ISyntaxWriter writer)
    {
        writer.Write(LeftSquareBracket);
        Visit(node.ElementSelection, writer);
        writer.Write(RightSquareBracket);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        PathObjectValueSelectionNode node,
        ISyntaxWriter writer)
    {
        Visit(node.Path, writer);
        writer.Write(Period);
        Visit(node.ObjectValueSelection, writer);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectValueSelectionNode node,
        ISyntaxWriter writer)
    {
        writer.Write(LeftBrace);
        WriteLineOrSpace(writer);

        writer.Indent();

        if (node.Fields.Length > 0)
        {
            Visit(node.Fields[0], writer);

            for (var i = 1; i < node.Fields.Length; i++)
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
        ObjectFieldSelectionNode node,
        ISyntaxWriter writer)
    {
        writer.WriteIndent(options.Indented);

        writer.Write(node.Name.Value);

        if (node.ValueSelection is not null)
        {
            writer.Write($"{Colon} ");
            Visit(node.ValueSelection, writer);
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ChoiceValueSelectionNode node,
        ISyntaxWriter writer)
    {
        Visit(node.Branches[0], writer);

        for (var i = 1; i < node.Branches.Length; i++)
        {
            writer.Write($" {Pipe} ");
            Visit(node.Branches[i], writer);
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
