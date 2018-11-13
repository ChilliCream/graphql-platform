using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotChocolate.Language
{
    public class SchemaSerializer
        : SchemaSyntaxWalker<DocumentWriter>
    {
        private bool _indent = false;

        public SchemaSerializer()
        {
        }

        public SchemaSerializer(bool useIndentation)
        {
            _indent = useIndentation;
        }

        public override void Visit(DocumentNode node, DocumentWriter writer)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            VisitDocument(node, writer);
        }

        protected override void VisitDocument(
            DocumentNode node,
            DocumentWriter writer)
        {
            if (node.Definitions.Any())
            {
                VisitDefinition(node.Definitions.First(), writer);

                foreach (IDefinitionNode item in node.Definitions.Skip(1))
                {
                    if (_indent)
                    {
                        writer.WriteLine();
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteSpace();
                    }

                    VisitDefinition(item, writer);
                }
            }
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Type);
            writer.WriteSpace();
            VisitName(node.Name, writer);

            if (node.Interfaces.Count > 0)
            {

            }

            // implements Character
            WriteDirectives(node.Directives, writer);

            WriteLeftBrace(writer);

            writer.Indent();
            writer.WriteMany(
                node.Fields,
                VisitFieldDefinition,
                WriteLineOrSpace);
            writer.Unindent();

            WriteRightBrace(writer);
        }

        protected override void VisitInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node, DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Type);
            writer.WriteSpace();
            VisitName(node.Name, writer);

            WriteDirectives(node.Directives, writer);

            WriteLeftBrace(writer);

            writer.Indent();
            writer.WriteMany(
                node.Fields,
                VisitFieldDefinition,
                WriteLineOrSpace);
            writer.Unindent();

            WriteRightBrace(writer);
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Union);
            writer.WriteSpace();
            VisitName(node.Name, writer);

            WriteDirectives(node.Directives, writer);

            writer.WriteSpace();
            writer.Write('=');
            writer.WriteSpace();

            writer.WriteMany(node.Types, VisitNamedType, " | ");
        }

        protected override void VisitFieldDefinition(
            FieldDefinitionNode node,
            DocumentWriter writer)
        {
            WriteIndentation(writer);

            WriteDescription(node.Description, writer);

            VisitName(node.Name, writer);

            if (node.Arguments.Any())
            {
                writer.Write("(");
                writer.WriteMany(
                    node.Arguments,
                    VisitInputValueDefinition,
                    ", ");
                writer.Write(")");
            }

            writer.Write(":");
            writer.WriteSpace();

            VisitType(node.Type, writer);

            WriteDirectives(node.Directives, writer);
        }

        protected override void VisitInputValueDefinition(
            InputValueDefinitionNode node,
            DocumentWriter writer)
        {
            if (node.Description != null)
            {
                VisitStringValue(node.Description, writer);
                writer.WriteSpace();
            }

            VisitName(node.Name, writer);
            writer.Write(":");
            writer.WriteSpace();

            VisitType(node.Type, writer);

            if (!node.DefaultValue.IsNull())
            {
                writer.WriteSpace();
                writer.Write("=");
                writer.WriteSpace();
                VisitValue(node.DefaultValue, writer);
            }

            WriteDirectives(node.Directives, writer);
        }

        protected override void VisitIntValue(
            IntValueNode node,
            DocumentWriter writer)
        {
            writer.Write(node.Value);
        }

        protected override void VisitFloatValue(
            FloatValueNode node,
            DocumentWriter writer)
        {
            writer.Write(node.Value);
        }

        protected override void VisitStringValue(
            StringValueNode node,
            DocumentWriter writer)
        {
            if (node.Block)
            {
                writer.Write("\"\"\"");

                string[] lines = node.Value
                    .Replace("\"\"\"", "\\\"\"\"")
                    .Replace("\r", string.Empty)
                    .Split('\n');

                foreach (string line in lines)
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

        protected override void VisitBooleanValue(
            BooleanValueNode node,
            DocumentWriter writer)
        {
            writer.Write(node.Value.ToString().ToLowerInvariant());
        }

        protected override void VisitEnumValue(
            EnumValueNode node,
            DocumentWriter writer)
        {
            writer.Write(node.Value);
        }

        protected override void VisitNullValue(
            NullValueNode node,
            DocumentWriter writer)
        {
            writer.Write("null");
        }

        protected override void VisitListValue(
            ListValueNode node,
            DocumentWriter writer)
        {
            writer.Write("[ ");

            writer.WriteMany(node.Items, VisitValue);

            writer.Write(" ]");
        }

        protected override void VisitObjectValue(
            ObjectValueNode node,
            DocumentWriter writer)
        {
            writer.Write("{ ");

            writer.WriteMany(node.Fields, VisitObjectField);

            writer.Write(" }");
        }

        protected override void VisitObjectField(
            ObjectFieldNode node,
            DocumentWriter writer)
        {
            VisitName(node.Name, writer);

            writer.Write(": ");

            VisitValue(node.Value, writer);
        }

        protected override void VisitVariable(
            VariableNode node,
            DocumentWriter writer)
        {
            writer.Write('$');
            VisitName(node.Name, writer);
        }

        protected override void VisitDirective(
            DirectiveNode node,
            DocumentWriter writer)
        {
            writer.Write('@');

            VisitName(node.Name, writer);

            if (node.Arguments.Any())
            {
                writer.Write('(');

                writer.WriteMany(node.Arguments, VisitArgument);

                writer.Write(')');
            }
        }

        protected override void VisitArgument(
            ArgumentNode node,
            DocumentWriter writer)
        {
            VisitName(node.Name, writer);

            writer.Write(": ");

            VisitValue(node.Value, writer);
        }

        protected override void VisitNonNullType(
            NonNullTypeNode node,
            DocumentWriter writer)
        {
            VisitType(node.Type, writer);
            writer.Write('!');
        }

        protected override void VisitListType(
            ListTypeNode node,
            DocumentWriter writer)
        {
            writer.Write('[');
            VisitType(node.Type, writer);
            writer.Write(']');
        }

        protected override void VisitNamedType(
            NamedTypeNode node,
            DocumentWriter writer)
        {
            VisitName(node.Name, writer);
        }

        protected override void VisitName(
            NameNode node,
            DocumentWriter writer)
        {
            writer.Write(node.Value);
        }

        private void WriteDescription(
            StringValueNode description,
            DocumentWriter writer)
        {
            if (description != null)
            {
                VisitStringValue(description, writer);

                if (_indent)
                {
                    writer.WriteLine();
                }
                else
                {
                    writer.WriteSpace();
                }

                WriteIndentation(writer);
            }
        }

        private void WriteDirectives(
            IReadOnlyCollection<DirectiveNode> directives,
            DocumentWriter writer)
        {
            if (directives.Any())
            {
                writer.WriteSpace();
                writer.WriteMany(directives, VisitDirective, " ");
            }
        }

        private void WriteLeftBrace(DocumentWriter writer)
        {
            writer.WriteSpace();
            writer.Write("{");
            WriteLineOrSpace(writer);
        }

        private void WriteRightBrace(DocumentWriter writer)
        {
            WriteLineOrSpace(writer);
            writer.Write("}");
        }

        private void WriteLineOrSpace(DocumentWriter writer)
        {
            if (_indent)
            {
                writer.WriteLine();
            }
            else
            {
                writer.WriteSpace();
            }
        }

        private void WriteIndentation(DocumentWriter writer)
        {
            if (_indent)
            {
                writer.WriteIndentation();
            }
        }
    }
}
