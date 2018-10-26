using System;
using System.Linq;
using System.Text;

namespace HotChocolate.Language
{
    public class QuerySerializer
        : SyntaxVisitor<ISyntaxNode>
    {
        private readonly StringBuilder _result = new StringBuilder();
        private DocumentWriter _writer;
        private bool _indent = false;

        public QuerySerializer()
        {
        }

        public QuerySerializer(bool useIndentation)
        {
            _indent = useIndentation;
        }

        public string Value => _result.ToString();

        public override void Visit(ISyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node != null)
            {
                _result.Clear();
                using (_writer = new DocumentWriter(_result))
                {
                    VisitInternal(node);
                    _writer.Flush();
                }
            }
        }

        private void VisitInternal(ISyntaxNode node)
        {
            switch (node)
            {
                case IValueNode value:
                    VisitValue(value);
                    break;
                case DocumentNode value:
                    VisitDocument(value);
                    break;
                default:
                    throw new NotSupportedException(
                        "Only document node and value nodes are supported " +
                        "as start node.");
            }
        }

        protected override void VisitDocument(DocumentNode node)
        {
            if (node.Definitions.Any())
            {
                VisitDefinition(node.Definitions.First());

                foreach (IDefinitionNode item in node.Definitions.Skip(1))
                {
                    if (_indent)
                    {
                        _writer.WriteLine();
                        _writer.WriteLine();
                    }
                    else
                    {
                        _writer.WriteSpace();
                    }

                    VisitDefinition(item);
                }
            }
        }

        protected virtual void VisitDefinition(IDefinitionNode node)
        {
            switch (node)
            {
                case OperationDefinitionNode value:
                    VisitOperationDefinition(value);
                    break;
                case FragmentDefinitionNode value:
                    VisitFragmentDefinition(value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode node)
        {
            if (node.Name != null)
            {
                _writer.Write(node.Operation.ToString().ToLowerInvariant());
                _writer.WriteSpace();

                VisitName(node.Name);
                if (node.VariableDefinitions.Any())
                {
                    _writer.Write('(');

                    _writer.WriteMany(node.VariableDefinitions, VisitVariableDefinition);

                    _writer.Write(')');
                }

                _writer.WriteMany(node.Directives, VisitDirective, " ");

                _writer.WriteSpace();
            }

            VisitSelectionSet(node.SelectionSet);
        }

        protected override void VisitVariableDefinition(
            VariableDefinitionNode node)
        {
            VisitVariable(node.Variable);

            _writer.Write(": ");

            VisitType(node.Type);

            if (node.DefaultValue != null)
            {
                _writer.Write(" = ");
                VisitValue(node.DefaultValue);
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node)
        {
            _writer.Write(Keywords.Fragment);
            _writer.WriteSpace();

            VisitName(node.Name);
            _writer.WriteSpace();

            if (node.VariableDefinitions.Any())
            {
                _writer.Write('(');

                _writer.WriteMany(node.VariableDefinitions, VisitVariableDefinition);

                _writer.Write(')');
                _writer.WriteSpace();
            }

            _writer.Write(Keywords.On);
            _writer.WriteSpace();

            VisitNamedType(node.TypeCondition);

            _writer.WriteMany(node.Directives, VisitDirective);

            VisitSelectionSet(node.SelectionSet);
        }

        protected override void VisitSelectionSet(SelectionSetNode node)
        {
            if (node != null && node.Selections.Any())
            {
                _writer.Write('{');

                string separator;
                if (_indent)
                {
                    _writer.WriteLine();
                    _writer.Indent();
                    separator = Environment.NewLine;
                }
                else
                {
                    _writer.WriteSpace();
                    separator = " ";
                }

                _writer.WriteMany(node.Selections, VisitSelection, separator);

                if (_indent)
                {
                    _writer.WriteLine();
                    _writer.Unindent();
                }
                else
                {
                    _writer.WriteSpace();
                }

                _writer.WriteIndentation();
                _writer.Write('}');
            }
        }

        protected override void VisitField(FieldNode node)
        {
            _writer.WriteIndentation();

            if (node.Alias != null)
            {
                VisitName(node.Alias);
                _writer.Write(": ");
            }

            VisitName(node.Name);

            if (node.Arguments.Any())
            {
                _writer.Write('(');
                _writer.WriteMany(node.Arguments, VisitArgument);
                _writer.Write(')');
            }

            if (node.Directives.Any())
            {
                _writer.WriteSpace();
                _writer.WriteMany(node.Directives, VisitDirective, " ");
            }

            if (node.SelectionSet != null && node.SelectionSet.Selections.Any())
            {
                _writer.WriteSpace();
                VisitSelectionSet(node.SelectionSet);
            }
        }

        protected override void VisitFragmentSpread(FragmentSpreadNode node)
        {
            _writer.WriteIndentation();

            _writer.Write("... ");
            VisitName(node.Name);

            if (node.Directives.Any())
            {
                _writer.WriteMany(node.Directives, VisitDirective, " ");
            }
        }

        protected override void VisitInlineFragment(InlineFragmentNode node)
        {
            _writer.WriteIndentation();

            _writer.Write("... ");

            if (node.TypeCondition != null)
            {
                _writer.Write(Keywords.On);
                _writer.WriteSpace();

                VisitNamedType(node.TypeCondition);
                _writer.WriteSpace();
            }

            if (node.Directives.Any())
            {
                _writer.WriteMany(node.Directives, VisitDirective, " ");
                _writer.WriteSpace();
            }

            VisitSelectionSet(node.SelectionSet);
        }

        protected override void VisitIntValue(IntValueNode node)
        {
            _writer.Write(node.Value);
        }

        protected override void VisitFloatValue(FloatValueNode node)
        {
            _writer.Write(node.Value);
        }

        protected override void VisitStringValue(StringValueNode node)
        {
            if (node.Block)
            {
                _writer.Write("\"\"\"");

                string[] lines = node.Value
                    .Replace("\"\"\"", "\\\"\"\"")
                    .Replace("\r", string.Empty)
                    .Split('\n');

                foreach (string line in lines)
                {
                    _writer.WriteLine();
                    _writer.WriteIndentation();
                    _writer.Write(line);
                }

                _writer.WriteLine();
                _writer.WriteIndentation();
                _writer.Write("\"\"\"");
            }
            else
            {
                _writer.Write($"\"{node.Value}\"");
            }
        }

        protected override void VisitBooleanValue(BooleanValueNode node)
        {
            _writer.Write(node.Value.ToString().ToLowerInvariant());
        }

        protected override void VisitEnumValue(EnumValueNode node)
        {
            _writer.Write(node.Value);
        }

        protected override void VisitNullValue(NullValueNode node)
        {
            _writer.Write("null");
        }

        protected override void VisitListValue(ListValueNode node)
        {
            _writer.Write("[ ");

            _writer.WriteMany(node.Items, VisitValue);

            _writer.Write(" ]");
        }

        protected override void VisitObjectValue(ObjectValueNode node)
        {
            _writer.Write("{ ");

            _writer.WriteMany(node.Fields, VisitObjectField);

            _writer.Write(" }");
        }

        protected override void VisitObjectField(ObjectFieldNode node)
        {
            VisitName(node.Name);

            _writer.Write(": ");

            VisitValue(node.Value);
        }

        protected override void VisitVariable(VariableNode node)
        {
            _writer.Write('$');
            VisitName(node.Name);
        }

        protected override void VisitDirective(DirectiveNode node)
        {
            _writer.Write('@');

            VisitName(node.Name);

            if (node.Arguments.Any())
            {
                _writer.Write('(');

                _writer.WriteMany(node.Arguments, VisitArgument);

                _writer.Write(')');
            }
        }

        protected override void VisitArgument(ArgumentNode node)
        {
            VisitName(node.Name);

            _writer.Write(": ");

            VisitValue(node.Value);
        }

        protected override void VisitNonNullType(NonNullTypeNode node)
        {
            VisitType(node.Type);
            _writer.Write('!');
        }

        protected override void VisitListType(ListTypeNode node)
        {
            _writer.Write('[');
            VisitType(node.Type);
            _writer.Write(']');
        }

        protected override void VisitNamedType(NamedTypeNode node)
        {
            VisitName(node.Name);
        }

        protected override void VisitName(NameNode node)
        {
            _writer.Write(node.Value);
        }
    }
}
