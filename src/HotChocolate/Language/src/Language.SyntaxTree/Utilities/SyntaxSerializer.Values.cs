namespace HotChocolate.Language.Utilities;

public sealed partial class SyntaxSerializer
{
    private void WriteValue(IValueNode node, ISyntaxWriter writer)
    {
        switch (node.Kind)
        {
            case SyntaxKind.ListValue:
                WriteListValue((ListValueNode)node, writer);
                break;

            case SyntaxKind.ObjectValue:
                WriteObjectValue((ObjectValueNode)node, writer);
                break;

            default:
                writer.WriteValue(node, indented: false);
                break;
        }
    }

    private void WriteListValue(ListValueNode node, ISyntaxWriter writer)
    {
        if (!_indented || node.Items.Count == 0)
        {
            writer.WriteListValue(node, indented: false);
            return;
        }

        var flatWidth = MeasureFlatListValue(node);

        if (!ContainsBlockString(node) && writer.Column + flatWidth <= _printWidth)
        {
            writer.WriteListValue(node, indented: false);
            return;
        }

        writer.Write('[');
        writer.Indent();

        foreach (var item in node.Items)
        {
            writer.WriteLine();
            writer.WriteIndent();
            WriteValue(item, writer);
        }

        writer.WriteLine();
        writer.Unindent();
        writer.WriteIndent();
        writer.Write(']');
    }

    private void WriteObjectValue(ObjectValueNode node, ISyntaxWriter writer)
    {
        if (node.Fields.Count == 0)
        {
            writer.Write("{}");
            return;
        }

        if (!_indented)
        {
            writer.WriteObjectValue(node, indented: false);
            return;
        }

        var flatWidth = MeasureFlatObjectValue(node);

        if (!ContainsBlockString(node) && writer.Column + flatWidth <= _printWidth)
        {
            writer.Write("{ ");

            for (var i = 0; i < node.Fields.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(", ");
                }

                var field = node.Fields[i];
                writer.WriteName(field.Name);
                writer.Write(": ");
                writer.WriteValue(field.Value, indented: false);
            }

            writer.Write(" }");
            return;
        }

        writer.Write('{');
        writer.Indent();

        foreach (var field in node.Fields)
        {
            writer.WriteLine();
            writer.WriteIndent();
            WriteObjectField(field, writer);
        }

        writer.WriteLine();
        writer.Unindent();
        writer.WriteIndent();
        writer.Write('}');
    }

    private void WriteObjectField(ObjectFieldNode node, ISyntaxWriter writer)
    {
        writer.WriteName(node.Name);
        writer.Write(": ");
        WriteValue(node.Value, writer);
    }

    private void WriteArgument(ArgumentNode node, ISyntaxWriter writer)
    {
        writer.WriteName(node.Name);
        writer.Write(": ");
        WriteValue(node.Value, writer);
    }

    private int MeasureFlatListValue(ListValueNode node)
    {
        var writer = StringSyntaxWriter.Rent();

        try
        {
            writer.WriteListValue(node, indented: false);
            return writer.Column;
        }
        finally
        {
            StringSyntaxWriter.Return(writer);
        }
    }

    private int MeasureFlatObjectValue(ObjectValueNode node)
    {
        var writer = StringSyntaxWriter.Rent();

        try
        {
            writer.Write("{ ");

            for (var i = 0; i < node.Fields.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(", ");
                }

                var field = node.Fields[i];
                writer.WriteName(field.Name);
                writer.Write(": ");
                writer.WriteValue(field.Value, indented: false);
            }

            writer.Write(" }");
            return writer.Column;
        }
        finally
        {
            StringSyntaxWriter.Return(writer);
        }
    }

    private static bool ContainsBlockString(IValueNode? node)
    {
        switch (node)
        {
            case StringValueNode stringValue:
                return stringValue.Block;

            case ListValueNode listValue:
                for (var i = 0; i < listValue.Items.Count; i++)
                {
                    if (ContainsBlockString(listValue.Items[i]))
                    {
                        return true;
                    }
                }
                return false;

            case ObjectValueNode objectValue:
                for (var i = 0; i < objectValue.Fields.Count; i++)
                {
                    if (ContainsBlockString(objectValue.Fields[i].Value))
                    {
                        return true;
                    }
                }
                return false;

            default:
                return false;
        }
    }

    private static bool DirectivesContainBlockString(IReadOnlyList<DirectiveNode> directives)
    {
        for (var i = 0; i < directives.Count; i++)
        {
            if (ArgumentsContainBlockString(directives[i].Arguments))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ArgumentsContainBlockString(IReadOnlyList<ArgumentNode> arguments)
    {
        for (var i = 0; i < arguments.Count; i++)
        {
            if (ContainsBlockString(arguments[i].Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ArgumentDefinitionsContainBlockString(
        IReadOnlyList<InputValueDefinitionNode> arguments)
    {
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];

            if (argument.Description is { Block: true })
            {
                return true;
            }

            if (ContainsBlockString(argument.DefaultValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool VariableDefinitionsContainBlockString(
        IReadOnlyList<VariableDefinitionNode> variableDefinitions)
    {
        for (var i = 0; i < variableDefinitions.Count; i++)
        {
            var variableDefinition = variableDefinitions[i];

            if (variableDefinition.Description is { Block: true })
            {
                return true;
            }

            if (ContainsBlockString(variableDefinition.DefaultValue))
            {
                return true;
            }
        }

        return false;
    }
}
