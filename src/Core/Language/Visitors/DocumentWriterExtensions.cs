using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Language
{
    internal static class DocumentWriterExtensions
    {
        public static void WriteMany<T>(
            this DocumentWriter writer,
            IEnumerable<T> items,
            Action<T, DocumentWriter> action)
        {
            WriteMany(writer, items, action, ", ");
        }

        public static void WriteMany<T>(
            this DocumentWriter writer,
            IEnumerable<T> items,
            Action<T, DocumentWriter> action,
            string separator)
        {
            if (items.Any())
            {
                action(items.First(), writer);

                foreach (T item in items.Skip(1))
                {
                    writer.Write(separator);
                    action(item, writer);
                }
            }
        }

        public static void WriteMany<T>(
            this DocumentWriter writer,
            IEnumerable<T> items,
            Action<T, DocumentWriter> action,
            Action<DocumentWriter> separator)
        {
            if (items.Any())
            {
                action(items.First(), writer);

                foreach (T item in items.Skip(1))
                {
                    separator(writer);
                    action(item, writer);
                }
            }
        }

        public static void WriteValue(
            this DocumentWriter writer,
            IValueNode node)
        {
            if (node is null)
            {
                return;
            }

            switch (node)
            {
                case IntValueNode value:
                    WriteIntValue(writer, value);
                    break;
                case FloatValueNode value:
                    WriteFloatValue(writer, value);
                    break;
                case StringValueNode value:
                    WriteStringValue(writer, value);
                    break;
                case BooleanValueNode value:
                    WriteBooleanValue(writer, value);
                    break;
                case EnumValueNode value:
                    WriteEnumValue(writer, value);
                    break;
                case NullValueNode value:
                    WriteNullValue(writer, value);
                    break;
                case ListValueNode value:
                    WriteListValue(writer, value);
                    break;
                case ObjectValueNode value:
                    WriteObjectValue(writer, value);
                    break;
                case VariableNode value:
                    WriteVariable(writer, value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public static void WriteIntValue(
            this DocumentWriter writer,
            IntValueNode node)
        {
            writer.Write(node.Value);
        }

        public static void WriteFloatValue(
            this DocumentWriter writer,
            FloatValueNode node)
        {
            writer.Write(node.Value);
        }

        public static void WriteStringValue(
            this DocumentWriter writer,
            StringValueNode node)
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
                    writer.WriteIndentation();
                    writer.Write(line);
                }

                writer.WriteLine();
                writer.WriteIndentation();
                writer.Write("\"\"\"");
            }
            else
            {
                writer.Write($"\"{node.Value}\"");
            }
        }

        public static void WriteBooleanValue(
            this DocumentWriter writer,
            BooleanValueNode node)
        {
            writer.Write(node.Value.ToString().ToLowerInvariant());
        }

        public static void WriteEnumValue(
            this DocumentWriter writer,
            EnumValueNode node)
        {
            writer.Write(node.Value);
        }

        public static void WriteNullValue(
            this DocumentWriter writer,
            NullValueNode node)
        {
            writer.Write("null");
        }

        public static void WriteListValue(
            this DocumentWriter writer,
            ListValueNode node)
        {
            writer.Write("[ ");
            writer.WriteMany(node.Items, (n, w) => w.WriteValue(n));
            writer.Write(" ]");
        }

        public static void WriteObjectValue(
            this DocumentWriter writer,
            ObjectValueNode node)
        {
            writer.Write("{ ");
            writer.WriteMany(node.Fields, (n, w) => w.WriteObjectField(n));
            writer.Write(" }");
        }

        public static void WriteObjectField(
            this DocumentWriter writer,
            ObjectFieldNode node)
        {
            writer.WriteField(node.Name, node.Value);
        }

        public static void WriteVariable(
            this DocumentWriter writer,
            VariableNode node)
        {
            writer.Write('$');
            writer.Write(node.Name.Value);
        }

        public static void WriteField(
            this DocumentWriter writer,
            NameNode name,
            IValueNode value)
        {
            writer.Write(name.Value);
            writer.Write(": ");
            writer.WriteValue(value);
        }

        public static void WriteArgument(
            this DocumentWriter writer,
            ArgumentNode node)
        {
            writer.WriteField(node.Name, node.Value);
        }

        public static void WriteType(
            this DocumentWriter writer,
            ITypeNode node)
        {
            switch (node)
            {
                case NonNullTypeNode value:
                    writer.WriteNonNullType(value);
                    break;
                case ListTypeNode value:
                    writer.WriteListType(value);
                    break;
                case NamedTypeNode value:
                    writer.WriteNamedType(value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public static void WriteNonNullType(
            this DocumentWriter writer,
            NonNullTypeNode node)
        {
            writer.WriteType(node.Type);
            writer.Write('!');
        }

        public static void WriteListType(
            this DocumentWriter writer,
            ListTypeNode node)
        {
            writer.Write('[');
            writer.WriteType(node.Type);
            writer.Write(']');
        }

        public static void WriteNamedType(
            this DocumentWriter writer,
            NamedTypeNode node)
        {
            writer.WriteName(node.Name);
        }

        public static void WriteName(
            this DocumentWriter writer,
            NameNode node)
        {
            writer.Write(node.Value);
        }

        public static void WriteDirective(
            this DocumentWriter writer,
            DirectiveNode node)
        {
            writer.Write('@');

            writer.WriteName(node.Name);

            if (node.Arguments.Any())
            {
                writer.Write('(');

                writer.WriteMany(node.Arguments, (n, w) => w.WriteArgument(n));

                writer.Write(')');
            }
        }
    }
}
