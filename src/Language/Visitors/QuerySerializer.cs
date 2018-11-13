using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotChocolate.Language
{
    public class QuerySerializer
        : SyntaxVisitor<ISyntaxNode, DocumentWriter>
    {
        private readonly bool _indent;

        public QuerySerializer()
        {
        }

        public QuerySerializer(bool useIndentation)
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

            if (node != null)
            {
                VisitInternal(node, writer);
            }
        }

        private void VisitInternal(
            ISyntaxNode node,
            DocumentWriter writer)
        {
            switch (node)
            {
                case IValueNode value:
                    VisitValue(value, writer);
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

                VisitName(node.Name, writer);
                if (node.VariableDefinitions.Any())
                {
                    writer.Write('(');

                    writer.WriteMany(
                        node.VariableDefinitions,
                        VisitVariableDefinition);

                    writer.Write(')');
                }

                writer.WriteMany(node.Directives, VisitDirective, " ");

                writer.WriteSpace();
            }

            VisitSelectionSet(node.SelectionSet, writer);
        }

        protected override void VisitVariableDefinition(
            VariableDefinitionNode node,
            DocumentWriter writer)
        {
            VisitVariable(node.Variable, writer);

            writer.Write(": ");

            VisitType(node.Type, writer);

            if (node.DefaultValue != null)
            {
                writer.Write(" = ");
                VisitValue(node.DefaultValue, writer);
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node,
            DocumentWriter writer)
        {
            writer.Write(Keywords.Fragment);
            writer.WriteSpace();

            VisitName(node.Name, writer);
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

            VisitNamedType(node.TypeCondition, writer);

            writer.WriteMany(node.Directives, VisitDirective);

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
                VisitName(node.Alias, writer);
                writer.Write(": ");
            }

            VisitName(node.Name, writer);

            if (node.Arguments.Any())
            {
                writer.Write('(');
                writer.WriteMany(node.Arguments, VisitArgument);
                writer.Write(')');
            }

            if (node.Directives.Any())
            {
                writer.WriteSpace();
                writer.WriteMany(node.Directives, VisitDirective, " ");
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
            VisitName(node.Name, writer);

            if (node.Directives.Any())
            {
                writer.WriteMany(node.Directives, VisitDirective, " ");
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

                VisitNamedType(node.TypeCondition, writer);
            }

            if (node.Directives.Any())
            {
                writer.WriteSpace();
                writer.WriteMany(node.Directives, VisitDirective, " ");
            }

            if (node.SelectionSet != null)
            {
                writer.WriteSpace();
                VisitSelectionSet(node.SelectionSet, writer);
            }
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
            WriteField(node.Name, node.Value, writer);
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

        protected override void VisitArgument(ArgumentNode node, DocumentWriter writer)
        {
            WriteField(node.Name, node.Value, writer);
        }

        protected override void VisitNonNullType(NonNullTypeNode node, DocumentWriter writer)
        {
            VisitType(node.Type, writer);
            writer.Write('!');
        }

        protected override void VisitListType(ListTypeNode node, DocumentWriter writer)
        {
            writer.Write('[');
            VisitType(node.Type, writer);
            writer.Write(']');
        }

        protected override void VisitNamedType(NamedTypeNode node, DocumentWriter writer)
        {
            VisitName(node.Name, writer);
        }

        protected override void VisitName(NameNode node, DocumentWriter writer)
        {
            writer.Write(node.Value);
        }

        private void WriteField(NameNode name, IValueNode value, DocumentWriter writer)
        {
            VisitName(name, writer);

            writer.Write(": ");

            VisitValue(value, writer);
        }
    }
}
