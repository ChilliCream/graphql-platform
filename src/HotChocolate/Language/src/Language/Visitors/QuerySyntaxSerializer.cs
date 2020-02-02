using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HotChocolate.Language
{
    public class QuerySyntaxSerializer
        : SyntaxVisitor<ISyntaxNode, DocumentWriter>
    {
        private readonly bool _indent;

        public QuerySyntaxSerializer()
        {
        }

        public QuerySyntaxSerializer(bool useIndentation)
        {
            _indent = useIndentation;
        }

        public override void Visit(
            ISyntaxNode node,
            DocumentWriter writer)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            VisitInternal(node, writer);
        }

        private void VisitInternal(
            ISyntaxNode node,
            DocumentWriter writer)
        {
            switch (node)
            {
                case IValueNode value:
                    writer.WriteValue(value);
                    break;

                case ArgumentNode argument:
                    writer.WriteArgument(argument);
                    break;

                case DocumentNode value:
                    VisitDocument(value, writer);
                    break;

                default:
                    throw new NotSupportedException(
                        "Only document node and value nodes are supported " +
                        "as start node.");
            }
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

        protected virtual void VisitDefinition(
            IDefinitionNode node,
            DocumentWriter writer)
        {
            switch (node)
            {
                case OperationDefinitionNode value:
                    VisitOperationDefinition(value, writer);
                    break;
                case FragmentDefinitionNode value:
                    VisitFragmentDefinition(value, writer);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode node,
            DocumentWriter writer)
        {
            if (node.Name != null)
            {
                writer.Write(node.Operation.ToString().ToLowerInvariant());
                writer.WriteSpace();

                writer.WriteName(node.Name);
                if (node.VariableDefinitions.Any())
                {
                    writer.Write('(');

                    writer.WriteMany(
                        node.VariableDefinitions,
                        VisitVariableDefinition);

                    writer.Write(')');
                }

                writer.WriteMany(node.Directives,
                    (n, w) => w.WriteDirective(n),
                    w => w.WriteSpace());

                writer.WriteSpace();
            }
            else if (node.Operation != OperationType.Query)
            {
                writer.Write(node.Operation.ToString().ToLowerInvariant());
                writer.WriteSpace();
            }

            VisitSelectionSet(node.SelectionSet, writer);
        }

        protected override void VisitVariableDefinition(
            VariableDefinitionNode node,
            DocumentWriter writer)
        {
            writer.WriteVariable(node.Variable);

            writer.Write(": ");

            writer.WriteType(node.Type);

            if (node.DefaultValue != null)
            {
                writer.Write(" = ");
                writer.WriteValue(node.DefaultValue);
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node,
            DocumentWriter writer)
        {
            writer.Write(Keywords.Fragment);
            writer.WriteSpace();

            writer.WriteName(node.Name);
            writer.WriteSpace();

            if (node.VariableDefinitions.Any())
            {
                writer.Write('(');

                writer.WriteMany(
                    node.VariableDefinitions,
                    VisitVariableDefinition);

                writer.Write(')');
                writer.WriteSpace();
            }

            writer.Write(Keywords.On);
            writer.WriteSpace();

            writer.WriteNamedType(node.TypeCondition);

            writer.WriteMany(node.Directives,
                (n, w) => w.WriteDirective(n));

            if (node.SelectionSet != null)
            {
                writer.WriteSpace();
                VisitSelectionSet(node.SelectionSet, writer);
            }
        }

        protected override void VisitSelectionSet(
            SelectionSetNode node,
            DocumentWriter writer)
        {
            if (node != null && node.Selections.Any())
            {
                writer.Write('{');

                string separator;
                if (_indent)
                {
                    writer.WriteLine();
                    writer.Indent();
                    separator = Environment.NewLine;
                }
                else
                {
                    writer.WriteSpace();
                    separator = " ";
                }

                writer.WriteMany(node.Selections, VisitSelection, separator);

                if (_indent)
                {
                    writer.WriteLine();
                    writer.Unindent();
                }
                else
                {
                    writer.WriteSpace();
                }

                writer.WriteIndentation();
                writer.Write('}');
            }
        }

        protected override void VisitField(
            FieldNode node,
            DocumentWriter writer)
        {
            writer.WriteIndentation();

            if (node.Alias != null)
            {
                writer.WriteName(node.Alias);
                writer.Write(": ");
            }

            writer.WriteName(node.Name);

            if (node.Arguments.Any())
            {
                writer.Write('(');
                writer.WriteMany(node.Arguments, (n, w) => w.WriteArgument(n));
                writer.Write(')');
            }

            if (node.Directives.Any())
            {
                writer.WriteSpace();
                writer.WriteMany(node.Directives,
                    (n, w) => w.WriteDirective(n),
                    w => w.WriteSpace());
            }

            if (node.SelectionSet != null && node.SelectionSet.Selections.Any())
            {
                writer.WriteSpace();
                VisitSelectionSet(node.SelectionSet, writer);
            }
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode node,
            DocumentWriter writer)
        {
            writer.WriteIndentation();

            writer.Write("... ");
            writer.WriteName(node.Name);

            if (node.Directives.Any())
            {
                writer.WriteMany(node.Directives,
                    (n, w) => w.WriteDirective(n),
                    w => w.WriteSpace());
            }
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode node,
            DocumentWriter writer)
        {
            writer.WriteIndentation();

            writer.Write("...");

            if (node.TypeCondition != null)
            {
                writer.WriteSpace();
                writer.Write(Keywords.On);
                writer.WriteSpace();

                writer.WriteNamedType(node.TypeCondition);
            }

            if (node.Directives.Any())
            {
                writer.WriteSpace();
                writer.WriteMany(node.Directives,
                    (n, w) => w.WriteDirective(n),
                    w => w.WriteSpace());
            }

            if (node.SelectionSet != null)
            {
                writer.WriteSpace();
                VisitSelectionSet(node.SelectionSet, writer);
            }
        }

        public static string Serialize(ISyntaxNode node) =>
            Serialize(node, true);

        public static string Serialize(ISyntaxNode node, bool useIndentation)
        {
            var text = new StringBuilder();
            Serialize(node, new StringWriter(text), useIndentation);
            return text.ToString();
        }

        public static void Serialize(
            ISyntaxNode node,
            TextWriter writer) =>
            Serialize(node, writer, true);

        public static void Serialize(
            ISyntaxNode node,
            TextWriter writer,
            bool useIndentation)
        {
            var serializer = new QuerySyntaxSerializer(useIndentation);
            serializer.Visit(node, new DocumentWriter(writer));
        }

        public static void Serialize(
            ISyntaxNode node,
            Stream stream,
            bool useIndentation)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                Serialize(node, writer, useIndentation);
            }
        }
    }
}
