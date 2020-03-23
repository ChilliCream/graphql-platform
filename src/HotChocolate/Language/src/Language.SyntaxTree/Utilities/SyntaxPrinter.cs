using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Language.Utilities
{
    public sealed class SyntaxPrinter
    {

    }



    public interface ISyntaxWriter
    {
        void Indent();

        void Unindent();

        void Write();

        void Write(char c);

        void Write(string s);

        void WriteLine(string s);

        void WriteLine(bool condition = true);

        void WriteSpace(bool condition = true);

        void WriteIndent(bool condition = true);
    }

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

                for (int i = 1; i < items.Count; i++)
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

                for (int i = 1; i < items.Count; i++)
                {
                    separator(writer);
                    action(items[i], writer);
                }
            }
        }

        public static void WriteValue(
            this ISyntaxWriter writer,
            IValueNode node)
        {
            if (node is null)
            {
                return;
            }

            switch (node.Kind)
            {
                case NodeKind.IntValue:
                    WriteIntValue(writer, (IntValueNode)node);
                    break;
                case NodeKind.FloatValue:
                    WriteFloatValue(writer, (FloatValueNode)node);
                    break;
                case NodeKind.StringValue:
                    WriteStringValue(writer, (StringValueNode)node);
                    break;
                case NodeKind.BooleanValue:
                    WriteBooleanValue(writer, (BooleanValueNode)node);
                    break;
                case NodeKind.EnumValue:
                    WriteEnumValue(writer, (EnumValueNode)node);
                    break;
                case NodeKind.NullValue:
                    WriteNullValue(writer);
                    break;
                case NodeKind.ListValue:
                    WriteListValue(writer, (ListValueNode)node);
                    break;
                case NodeKind.ObjectValue:
                    WriteObjectValue(writer, (ObjectValueNode)node);
                    break;
                case NodeKind.Variable:
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
                writer.Write($"\"{WriteEscapeCharacters(node.Value)}\"");
            }
        }


        private static string WriteEscapeCharacters(string input)
        {
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                WriteEscapeCharacter(stringBuilder, in c);
            }

            return stringBuilder.ToString();
        }

        private static void WriteEscapeCharacter(
            StringBuilder stringBuilder, in char c)
        {
            switch (c)
            {
                case '"':
                    WriteEscapeCharacterHelper(stringBuilder, '"');
                    break;
                case '\\':
                    WriteEscapeCharacterHelper(stringBuilder, '\\');
                    break;
                case '/':
                    WriteEscapeCharacterHelper(stringBuilder, '/');
                    break;
                case '\b':
                    WriteEscapeCharacterHelper(stringBuilder, 'b');
                    break;
                case '\f':
                    WriteEscapeCharacterHelper(stringBuilder, 'f');
                    break;
                case '\n':
                    WriteEscapeCharacterHelper(stringBuilder, 'n');
                    break;
                case '\r':
                    WriteEscapeCharacterHelper(stringBuilder, 'r');
                    break;
                case '\t':
                    WriteEscapeCharacterHelper(stringBuilder, 't');
                    break;
                default:
                    stringBuilder.Append(c);
                    break;
            }
        }

        private static void WriteEscapeCharacterHelper(StringBuilder stringBuilder, in char c)
        {
            stringBuilder.Append('\\');
            stringBuilder.Append(c);
        }

        public static void WriteBooleanValue(this ISyntaxWriter writer, BooleanValueNode node)
        {
            writer.Write(node.Value.ToString().ToLowerInvariant());
        }

        public static void WriteEnumValue(this ISyntaxWriter writer, EnumValueNode node)
        {
            writer.Write(node.Value);
        }

        public static void WriteNullValue(this ISyntaxWriter writer)
        {
            writer.Write("null");
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
                case NodeKind.NonNullType:
                    writer.WriteNonNullType((NonNullTypeNode)node);
                    break;
                case NodeKind.ListType:
                    writer.WriteListType((ListTypeNode)node);
                    break;
                case NodeKind.NamedType:
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
}
