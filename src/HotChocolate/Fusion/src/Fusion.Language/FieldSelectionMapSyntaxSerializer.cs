using System.Collections.Immutable;
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

        var arguments = node.GetArguments();

        if (arguments.Count > 0)
        {
            writer.Write(LeftParenthesis);
            writer.Write(arguments[0].ToString());

            for (var i = 1; i < arguments.Count; i++)
            {
                writer.Write($", {arguments[i]}");
            }

            writer.Write(RightParenthesis);
        }

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

        WriteArguments(node.Arguments, writer);

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

    protected override ISyntaxVisitorAction Enter(
        ArgumentNode node,
        ISyntaxWriter writer)
    {
        writer.Write(node.Name.Value);
        writer.Write($"{Colon} ");
        Visit(node.Value, writer);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectFieldNode node,
        ISyntaxWriter writer)
    {
        writer.Write(node.Name.Value);
        writer.Write($"{Colon} ");
        Visit(node.Value, writer);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        IntValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write(node.Value);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        FloatValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write(node.Value);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        StringValueNode node,
        ISyntaxWriter writer)
    {
        if (node.Block)
        {
            // Block strings are printed in HotChocolate's multi-line triple-quote layout so the
            // two serializers stay in sync. The layout uses real newlines even in compact mode.
            var lines = node.Value
                .Replace("\"\"\"", "\\\"\"\"")
                .Replace("\r", string.Empty)
                .Split('\n');

            if (lines.Length == 1)
            {
                lines[0] = lines[0].Trim();
            }

            writer.Write("\"\"\"");

            foreach (var line in lines)
            {
                writer.WriteLine();
                writer.WriteIndent();
                writer.Write(line);
            }

            writer.WriteLine();
            writer.WriteIndent();
            writer.Write("\"\"\"");
        }
        else
        {
            // A regular string is printed in its canonical double-quoted form with escape
            // sequences. The escaping mirrors HotChocolate's WriteEscapeCharacter so both stay
            // in sync.
            writer.Write(Quote);
            WriteEscapedString(node.Value, writer);
            writer.Write(Quote);
        }

        return Skip;
    }

    private static void WriteEscapedString(string value, ISyntaxWriter writer)
    {
        foreach (var c in value)
        {
            switch (c)
            {
                case Quote:
                    writer.Write(Backslash);
                    writer.Write(Quote);
                    break;

                case Backslash:
                    writer.Write(Backslash);
                    writer.Write(Backslash);
                    break;

                case '\b':
                    writer.Write(Backslash);
                    writer.Write('b');
                    break;

                case '\f':
                    writer.Write(Backslash);
                    writer.Write('f');
                    break;

                case LineFeed:
                    writer.Write(Backslash);
                    writer.Write('n');
                    break;

                case Return:
                    writer.Write(Backslash);
                    writer.Write('r');
                    break;

                case HorizontalTab:
                    writer.Write(Backslash);
                    writer.Write('t');
                    break;

                default:
                    writer.Write(c);
                    break;
            }
        }
    }

    protected override ISyntaxVisitorAction Enter(
        BooleanValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write(node.Value ? "true" : "false");

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        NullValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write("null");

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        EnumValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write(node.Value);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ListValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write(LeftSquareBracket);

        if (node.Items.Length > 0)
        {
            Visit(node.Items[0], writer);

            for (var i = 1; i < node.Items.Length; i++)
            {
                writer.Write(", ");
                Visit(node.Items[i], writer);
            }
        }

        writer.Write(RightSquareBracket);

        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectValueNode node,
        ISyntaxWriter writer)
    {
        writer.Write(LeftBrace);

        if (node.Fields.Length > 0)
        {
            writer.WriteSpace();

            Visit(node.Fields[0], writer);

            for (var i = 1; i < node.Fields.Length; i++)
            {
                writer.Write(", ");
                Visit(node.Fields[i], writer);
            }

            writer.WriteSpace();
        }

        writer.Write(RightBrace);

        return Skip;
    }

    private void WriteArguments(
        ImmutableArray<ArgumentNode> arguments,
        ISyntaxWriter writer)
    {
        if (arguments.Length == 0)
        {
            return;
        }

        writer.Write(LeftParenthesis);

        Visit(arguments[0], writer);

        for (var i = 1; i < arguments.Length; i++)
        {
            writer.Write(", ");
            Visit(arguments[i], writer);
        }

        writer.Write(RightParenthesis);
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
