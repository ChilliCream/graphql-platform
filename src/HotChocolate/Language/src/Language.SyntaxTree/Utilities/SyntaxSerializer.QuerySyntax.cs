namespace HotChocolate.Language.Utilities;

public sealed partial class SyntaxSerializer
{
    private void VisitOperationDefinition(
        OperationDefinitionNode node,
        ISyntaxWriter writer)
    {
        var writeOperation = node.Name is not null
            || node.Operation != OperationType.Query
            || node.VariableDefinitions.Count > 0
            || node.Directives.Count > 0;

        if (writeOperation)
        {
            WriteDescription(node.Description, writer);
            writer.Write(node.Operation.ToString().ToLowerInvariant());
        }

        if (node.Name is not null)
        {
            writer.WriteSpace();
            writer.WriteName(node.Name);
        }

        if (node.VariableDefinitions.Count > 0)
        {
            writer.Write('(');

            string separator;
            if (_indented)
            {
                writer.WriteLine();
                writer.Indent();
                separator = Environment.NewLine;
            }
            else
            {
                separator = ", ";
            }

            writer.WriteMany(node.VariableDefinitions, VisitVariableDefinition, separator);

            if (_indented)
            {
                writer.WriteLine();
                writer.Unindent();
            }

            writer.WriteIndent();
            writer.Write(')');
        }

        WriteDirectives(node.Directives, writer);

        if (writeOperation)
        {
            writer.WriteSpace();
        }
        VisitSelectionSet(node.SelectionSet, writer);
    }

    private void VisitVariableDefinition(VariableDefinitionNode node, ISyntaxWriter writer)
    {
        writer.WriteIndent();

        WriteDescription(node.Description, writer);

        writer.WriteVariable(node.Variable);

        writer.Write(": ");

        writer.WriteType(node.Type);

        if (node.DefaultValue is not null)
        {
            writer.Write(" = ");
            writer.WriteValue(node.DefaultValue, _indented);
        }

        WriteDirectives(node.Directives, writer);
    }

    private void VisitFragmentDefinition(FragmentDefinitionNode node, ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);

        writer.Write(Keywords.Fragment);
        writer.WriteSpace();

        writer.WriteName(node.Name);
        writer.WriteSpace();

        if (node.VariableDefinitions.Count > 0)
        {
            writer.Write('(');

            string separator;
            if (_indented)
            {
                writer.WriteLine();
                writer.Indent();
                separator = Environment.NewLine;
            }
            else
            {
                separator = ", ";
            }

            writer.WriteMany(
                node.VariableDefinitions,
                VisitVariableDefinition,
                separator);

            if (_indented)
            {
                writer.WriteLine();
                writer.Unindent();
            }

            writer.WriteIndent();
            writer.Write(')');
            writer.WriteSpace();
        }

        writer.Write(Keywords.On);
        writer.WriteSpace();

        writer.WriteNamedType(node.TypeCondition);

        WriteDirectives(node.Directives, writer);

        writer.WriteSpace();
        VisitSelectionSet(node.SelectionSet, writer);
    }

    private void VisitSelectionSet(SelectionSetNode node, ISyntaxWriter writer)
    {
        writer.Write('{');

        string separator;
        if (_indented)
        {
            writer.WriteLine();
            writer.Indent();
            writer.WriteIndent();
            separator = Environment.NewLine;
        }
        else
        {
            writer.WriteSpace();
            separator = " ";
        }

        writer.WriteMany(node.Selections, VisitSelection, separator);

        if (_indented)
        {
            writer.WriteLine();
            writer.Unindent();
        }
        else
        {
            writer.WriteSpace();
        }

        writer.WriteIndent();
        writer.Write('}');
    }

    private void VisitSelection(ISelectionNode node, ISyntaxWriter context)
    {
        switch (node.Kind)
        {
            case SyntaxKind.Field:
                VisitField((FieldNode)node, context);
                break;
            case SyntaxKind.FragmentSpread:
                VisitFragmentSpread((FragmentSpreadNode)node, context);
                break;
            case SyntaxKind.InlineFragment:
                VisitInlineFragment((InlineFragmentNode)node, context);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    private void VisitField(FieldNode node, ISyntaxWriter writer)
    {
        if (node.Alias is not null)
        {
            writer.WriteName(node.Alias);
            writer.Write(": ");
        }

        writer.WriteName(node.Name);

        if (node.Arguments.Count > 0)
        {
            writer.Write('(');
            writer.WriteMany(node.Arguments, (n, w) => w.WriteArgument(n));
            writer.Write(')');
        }

        WriteDirectives(node.Directives, writer);

        if (node.SelectionSet is not null)
        {
            writer.WriteSpace();
            VisitSelectionSet(node.SelectionSet, writer);
        }
    }

    private void VisitFragmentSpread(FragmentSpreadNode node, ISyntaxWriter writer)
    {
        writer.Write("... ");
        writer.WriteName(node.Name);

        WriteDirectives(node.Directives, writer);
    }

    private void VisitInlineFragment(InlineFragmentNode node, ISyntaxWriter writer)
    {
        writer.Write("...");

        if (node.TypeCondition is not null)
        {
            writer.WriteSpace();
            writer.Write(Keywords.On);
            writer.WriteSpace();

            writer.WriteNamedType(node.TypeCondition);
        }

        WriteDirectives(node.Directives, writer);

        writer.WriteSpace();
        VisitSelectionSet(node.SelectionSet, writer);
    }
}
