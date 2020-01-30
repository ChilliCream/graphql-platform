using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HotChocolate.Language
{
    public class SchemaSyntaxSerializer
        : SchemaSyntaxWalker<DocumentWriter>
    {
        private readonly bool _indent;

        public SchemaSyntaxSerializer()
        {
        }

        public SchemaSyntaxSerializer(bool useIndentation)
        {
            _indent = useIndentation;
        }

        public override void Visit(
            DocumentNode node,
            DocumentWriter writer)
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

        protected override void VisitSchemaDefinition(
            SchemaDefinitionNode node,
            DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Schema);
            WriteDirectives(node.Directives, writer);

            WriteLeftBrace(writer);

            writer.Indent();
            writer.WriteMany(
                node.OperationTypes,
                VisitOperationTypeDefinition,
                WriteLineOrSpace);
            writer.Unindent();

            WriteRightBrace(writer);
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Type);
            writer.WriteSpace();
            writer.WriteName(node.Name);

            if (node.Interfaces.Count > 0)
            {
                writer.WriteSpace();
                writer.Write(Keywords.Implements);
                writer.WriteSpace();
                writer.WriteMany(node.Interfaces,
                    (n, w) => writer.WriteNamedType(n),
                    " & ");
            }

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
            InterfaceTypeDefinitionNode node,
            DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Interface);
            writer.WriteSpace();
            writer.WriteName(node.Name);

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
            writer.WriteName(node.Name);

            WriteDirectives(node.Directives, writer);

            writer.WriteSpace();
            writer.Write('=');
            writer.WriteSpace();

            writer.WriteMany(node.Types,
                (n, w) => writer.WriteNamedType(n),
                " | ");
        }

        protected override void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Enum);
            writer.WriteSpace();
            writer.WriteName(node.Name);

            WriteDirectives(node.Directives, writer);

            WriteLeftBrace(writer);

            writer.Indent();
            writer.WriteMany(
                node.Values,
                VisitEnumValueDefinition,
                WriteLineOrSpace);
            writer.Unindent();

            WriteRightBrace(writer);
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Input);
            writer.WriteSpace();
            writer.WriteName(node.Name);

            WriteDirectives(node.Directives, writer);

            WriteLeftBrace(writer);

            writer.Indent();
            writer.WriteMany(
                node.Fields,
                VisitInputValueDefinition,
                WriteLineOrSpace);
            writer.Unindent();

            WriteRightBrace(writer);
        }

        protected override void VisitScalarTypeDefinition(
           ScalarTypeDefinitionNode node,
           DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Scalar);
            writer.WriteSpace();
            writer.WriteName(node.Name);

            WriteDirectives(node.Directives, writer);
        }

        protected override void VisitOperationTypeDefinition(
            OperationTypeDefinitionNode node,
            DocumentWriter writer)
        {
            WriteIndentation(writer);

            writer.Write(node.Operation.ToString().ToLowerInvariant());
            writer.Write(": ");
            writer.WriteNamedType(node.Type);
        }

        protected override void VisitFieldDefinition(
            FieldDefinitionNode node,
            DocumentWriter writer)
        {
            WriteIndentation(writer);

            WriteDescription(node.Description, writer);

            writer.WriteName(node.Name);

            if (node.Arguments.Any())
            {
                writer.Write("(");
                writer.WriteMany(
                    node.Arguments,
                    VisitArgumentValueDefinition,
                    w => w.WriteSpace());
                writer.Write(")");
            }

            writer.Write(":");
            writer.WriteSpace();

            writer.WriteType(node.Type);

            WriteDirectives(node.Directives, writer);
        }

        protected override void VisitInputValueDefinition(
            InputValueDefinitionNode node,
            DocumentWriter writer)
        {
            WriteIndentation(writer);

            WriteDescription(node.Description, writer);

            WriteInputValueDefinition(node, writer);
        }

        protected override void VisitDirectiveDefinition(
            DirectiveDefinitionNode node,
            DocumentWriter writer)
        {
            WriteDescription(node.Description, writer);

            writer.Write(Keywords.Directive);
            writer.WriteSpace();
            writer.Write('@');
            writer.WriteName(node.Name);

            if (node.Arguments.Any())
            {
                writer.Write("(");
                writer.WriteMany(
                    node.Arguments,
                    VisitArgumentValueDefinition,
                    w => w.WriteSpace());
                writer.Write(")");
            }

            writer.WriteSpace();

            if (node.IsRepeatable)
            {
                writer.Write(Keywords.Repeatable);
                writer.WriteSpace();
            }

            writer.Write(Keywords.On);
            writer.WriteSpace();

            writer.WriteMany(node.Locations,
                (n, w) => writer.WriteName(n),
                " | ");
        }

        protected virtual void VisitArgumentValueDefinition(
           InputValueDefinitionNode node,
           DocumentWriter writer)
        {
            if (node.Description != null)
            {
                writer.WriteStringValue(node.Description);
                writer.WriteSpace();
            }

            WriteInputValueDefinition(node, writer);
        }

        private static void WriteInputValueDefinition(
           InputValueDefinitionNode node,
           DocumentWriter writer)
        {
            writer.WriteName(node.Name);
            writer.Write(":");
            writer.WriteSpace();

            writer.WriteType(node.Type);

            if (!node.DefaultValue.IsNull())
            {
                writer.WriteSpace();
                writer.Write("=");
                writer.WriteSpace();
                writer.WriteValue(node.DefaultValue);
            }

            WriteDirectives(node.Directives, writer);
        }

        protected override void VisitEnumValueDefinition(
            EnumValueDefinitionNode node,
            DocumentWriter writer)
        {
            WriteIndentation(writer);

            WriteDescription(node.Description, writer);

            writer.WriteName(node.Name);

            WriteDirectives(node.Directives, writer);
        }

        private void WriteDescription(
            StringValueNode? description,
            DocumentWriter writer)
        {
            if (description != null)
            {
                writer.WriteStringValue(description);

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

        private static void WriteDirectives(
            IReadOnlyList<DirectiveNode> directives,
            DocumentWriter writer)
        {
            if (directives.Any())
            {
                writer.WriteSpace();
                writer.WriteMany(directives,
                    (n, w) => w.WriteDirective(n),
                    w => w.WriteSpace());
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

        public static string Serialize(DocumentNode node) =>
            Serialize(node, true);

        public static string Serialize(DocumentNode node, bool useIndentation)
        {
            var text = new StringBuilder();
            Serialize(node, new StringWriter(text), useIndentation);
            return text.ToString();
        }

        public static void Serialize(
            DocumentNode node,
            TextWriter writer) =>
            Serialize(node, writer, true);

        public static void Serialize(
            DocumentNode node,
            TextWriter writer,
            bool useIndentation)
        {
            var serializer = new SchemaSyntaxSerializer(useIndentation);
            serializer.Visit(node, new DocumentWriter(writer));
        }

        public static void Serialize(
            DocumentNode node,
            Stream stream) =>
            Serialize(node, stream, true);

        public static void Serialize(
            DocumentNode node,
            Stream stream,
            bool useIndentation)
        {
            using var sr = new StreamWriter(stream);
            var serializer = new SchemaSyntaxSerializer(useIndentation);
            serializer.Visit(node, new DocumentWriter(sr));
        }
    }
}
