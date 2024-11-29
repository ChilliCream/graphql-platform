using static HotChocolate.Language.Properties.Resources;

namespace HotChocolate.Language.Utilities;

public static class SyntaxWriterExtensions
{
    public static void WriteName(this ISyntaxWriter writer, NameNode nameNode)
    {
        writer.Write(nameNode.Value);
    }

    public static void WriteMany<T>(
        this ISyntaxWriter writer,
        IReadOnlyList<T> items,
        Action<T, ISyntaxWriter> action)
    {
        WriteMany(writer, items, action, ", ");
    }

    public static void WriteMany<T>(
        this ISyntaxWriter writer,
        IReadOnlyList<T> items,
        Action<T, ISyntaxWriter> action,
        string separator)
    {
        if (items.Count > 0)
        {
            action(items[0], writer);

            for (var i = 1; i < items.Count; i++)
            {
                writer.Write(separator);
                action(items[i], writer);
            }
        }
    }

    public static void WriteMany<T>(
        this ISyntaxWriter writer,
        IReadOnlyList<T> items,
        Action<T, ISyntaxWriter> action,
        Action<ISyntaxWriter> separator)
    {
        if (items.Count > 0)
        {
            action(items[0], writer);

            for (var i = 1; i < items.Count; i++)
            {
                separator(writer);
                action(items[i], writer);
            }
        }
    }

    public static void WriteValue(
        this ISyntaxWriter writer,
        IValueNode? node)
    {
        if (node is null)
        {
            return;
        }

        switch (node.Kind)
        {
            case SyntaxKind.IntValue:
                WriteIntValue(writer, (IntValueNode)node);
                break;

            case SyntaxKind.FloatValue:
                WriteFloatValue(writer, (FloatValueNode)node);
                break;

            case SyntaxKind.StringValue:
                if (node is StringValueNode stringValueNode)
                {
                    WriteStringValue(writer, stringValueNode);
                }
                else if(node is IValueNode<string> stringLikeNode)
                {
                    WriteStringValue(writer, stringLikeNode.Value);
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format(
                            SyntaxWriterExtensions_WriteValue_ValueNodeNotSupported,
                            node.GetType().FullName));
                }
                break;

            case SyntaxKind.BooleanValue:
                WriteBooleanValue(writer, (BooleanValueNode)node);
                break;

            case SyntaxKind.EnumValue:
                WriteEnumValue(writer, (EnumValueNode)node);
                break;

            case SyntaxKind.NullValue:
                WriteNullValue(writer);
                break;

            case SyntaxKind.ListValue:
                WriteListValue(writer, (ListValueNode)node);
                break;

            case SyntaxKind.ObjectValue:
                WriteObjectValue(writer, (ObjectValueNode)node);
                break;

            case SyntaxKind.Variable:
                WriteVariable(writer, (VariableNode)node);
                break;

            default:
                throw new NotSupportedException();
        }
    }

    public static void WriteIntValue(this ISyntaxWriter writer, IntValueNode node)
    {
        writer.Write(node.Value);
    }

    public static void WriteFloatValue(this ISyntaxWriter writer, FloatValueNode node)
    {
        writer.Write(node.Value);
    }

    public static void WriteStringValue(this ISyntaxWriter writer, StringValueNode node)
    {
        if (node.Block)
        {
            writer.Write("\"\"\"");

            var lines = node.Value
                .Replace("\"\"\"", "\\\"\"\"")
                .Replace("\r", string.Empty)
                .Split('\n');

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
            writer.Write('"');
            WriteEscapeCharacters(writer, node.Value);
            writer.Write('"');
        }
    }

    public static void WriteStringValue(this ISyntaxWriter writer, string? value)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.Write('"');
        WriteEscapeCharacters(writer, value);
        writer.Write('"');
    }

    private static void WriteEscapeCharacters(ISyntaxWriter writer, string input)
    {
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            WriteEscapeCharacter(writer, in c);
        }
    }

    private static void WriteEscapeCharacter(
        ISyntaxWriter writer,
        in char c)
    {
        switch (c)
        {
            case '"':
                WriteEscapeCharacterHelper(writer, '"');
                break;

            case '\\':
                WriteEscapeCharacterHelper(writer, '\\');
                break;

            case '/':
                WriteEscapeCharacterHelper(writer, '/');
                break;

            case '\b':
                WriteEscapeCharacterHelper(writer, 'b');
                break;

            case '\f':
                WriteEscapeCharacterHelper(writer, 'f');
                break;

            case '\n':
                WriteEscapeCharacterHelper(writer, 'n');
                break;

            case '\r':
                WriteEscapeCharacterHelper(writer, 'r');
                break;

            case '\t':
                WriteEscapeCharacterHelper(writer, 't');
                break;

            default:
                writer.Write(c);
                break;
        }
    }

    private static void WriteEscapeCharacterHelper(ISyntaxWriter writer, in char c)
    {
        writer.Write('\\');
        writer.Write(c);
    }

    public static void WriteBooleanValue(this ISyntaxWriter writer, BooleanValueNode node)
    {
        writer.Write(
            node.Value
                ? Keywords.True
                : Keywords.False);
    }

    public static void WriteEnumValue(this ISyntaxWriter writer, EnumValueNode node)
    {
        writer.Write(node.Value);
    }

    public static void WriteNullValue(this ISyntaxWriter writer)
    {
        writer.Write(Keywords.Null);
    }

    public static void WriteListValue(this ISyntaxWriter writer, ListValueNode node)
    {
        writer.Write("[ ");
        writer.WriteMany(node.Items, (n, w) => w.WriteValue(n));
        writer.Write(" ]");
    }

    public static void WriteObjectValue(this ISyntaxWriter writer, ObjectValueNode node)
    {
        writer.Write("{ ");
        writer.WriteMany(node.Fields, (n, w) => w.WriteObjectField(n));
        writer.Write(" }");
    }

    public static void WriteObjectField(this ISyntaxWriter writer, ObjectFieldNode node)
    {
        writer.WriteField(node.Name, node.Value);
    }

    public static void WriteVariable(this ISyntaxWriter writer, VariableNode node)
    {
        writer.Write('$');
        writer.Write(node.Name.Value);
    }

    public static void WriteField(this ISyntaxWriter writer, NameNode name, IValueNode value)
    {
        writer.Write(name.Value);
        writer.Write(": ");
        writer.WriteValue(value);
    }

    public static void WriteArgument(this ISyntaxWriter writer, ArgumentNode node)
    {
        writer.WriteField(node.Name, node.Value);
    }

    public static void WriteType(this ISyntaxWriter writer, ITypeNode node)
    {
        switch (node.Kind)
        {
            case SyntaxKind.NonNullType:
                writer.WriteNonNullType((NonNullTypeNode)node);
                break;

            case SyntaxKind.ListType:
                writer.WriteListType((ListTypeNode)node);
                break;

            case SyntaxKind.NamedType:
                writer.WriteNamedType((NamedTypeNode)node);
                break;

            default:
                throw new NotSupportedException();
        }
    }

    public static void WriteNonNullType(this ISyntaxWriter writer, NonNullTypeNode node)
    {
        writer.WriteType(node.Type);
        writer.Write('!');
    }

    public static void WriteListType(this ISyntaxWriter writer, ListTypeNode node)
    {
        writer.Write('[');
        writer.WriteType(node.Type);
        writer.Write(']');
    }

    public static void WriteNamedType(this ISyntaxWriter writer, NamedTypeNode node)
    {
        writer.WriteName(node.Name);
    }

    public static void WriteDirective(this ISyntaxWriter writer, DirectiveNode node)
    {
        writer.Write('@');

        writer.WriteName(node.Name);

        if (node.Arguments.Count > 0)
        {
            writer.Write('(');
            writer.WriteMany(node.Arguments, (n, w) => w.WriteArgument(n));
            writer.Write(')');
        }
    }
}
