namespace HotChocolate.Language.Utilities;

public sealed partial class SyntaxSerializer
{
    private void VisitSchemaDefinition(SchemaDefinitionNode node, ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);
        VisitSchemaDefinitionBase(node, writer);
    }

    private void VisitSchemaExtension(SchemaExtensionNode node, ISyntaxWriter writer)
    {
        writer.Write(Keywords.Extend);
        writer.WriteSpace();
        VisitSchemaDefinitionBase(node, writer);
    }

    private void VisitSchemaDefinitionBase(SchemaDefinitionNodeBase node, ISyntaxWriter writer)
    {
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

    private void VisitObjectTypeDefinition(ObjectTypeDefinitionNode node, ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);
        VisitObjectTypeDefinitionBase(node, writer);
    }

    private void VisitObjectTypeExtension(ObjectTypeExtensionNode node, ISyntaxWriter writer)
    {
        writer.Write(Keywords.Extend);
        writer.WriteSpace();
        VisitObjectTypeDefinitionBase(node, writer);
    }

    private void VisitObjectTypeDefinitionBase(
        ComplexTypeDefinitionNodeBase node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Type);
        writer.WriteSpace();
        writer.WriteName(node.Name);

        if (node.Interfaces.Count > 0)
        {
            writer.WriteSpace();
            writer.Write(Keywords.Implements);
            writer.WriteSpace();
            WriteSeparatedList(
                node.Interfaces,
                " & ",
                " &",
                SeparatedListBreakStyle.Trailing,
                string.Empty,
                (n, w) => w.WriteNamedType(n),
                writer);
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

    private void VisitInterfaceTypeDefinition(
        InterfaceTypeDefinitionNode node,
        ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);
        VisitInterfaceTypeDefinitionBase(node, writer);
    }

    private void VisitInterfaceTypeExtension(
        InterfaceTypeExtensionNode node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Extend);
        writer.WriteSpace();
        VisitInterfaceTypeDefinitionBase(node, writer);
    }

    private void VisitInterfaceTypeDefinitionBase(
        ComplexTypeDefinitionNodeBase node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Interface);
        writer.WriteSpace();
        writer.WriteName(node.Name);

        if (node.Interfaces.Count > 0)
        {
            writer.WriteSpace();
            writer.Write(Keywords.Implements);
            writer.WriteSpace();
            WriteSeparatedList(
                node.Interfaces,
                " & ",
                " &",
                SeparatedListBreakStyle.Trailing,
                string.Empty,
                (n, w) => w.WriteNamedType(n),
                writer);
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

    private void VisitUnionTypeDefinition(
        UnionTypeDefinitionNode node,
        ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);
        VisitUnionTypeDefinitionBase(node, writer);
    }

    private void VisitUnionTypeExtension(
        UnionTypeExtensionNode node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Extend);
        writer.WriteSpace();
        VisitUnionTypeDefinitionBase(node, writer);
    }

    private void VisitUnionTypeDefinitionBase(
        UnionTypeDefinitionNodeBase node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Union);
        writer.WriteSpace();
        writer.WriteName(node.Name);

        WriteDirectives(node.Directives, writer);

        writer.WriteSpace();
        writer.Write('=');

        WriteSeparatedList(
            node.Types,
            " | ",
            "| ",
            SeparatedListBreakStyle.Leading,
            " ",
            (n, w) => w.WriteNamedType(n),
            writer);
    }

    private void VisitEnumTypeDefinition(
        EnumTypeDefinitionNode node,
        ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);
        VisitEnumTypeDefinitionBase(node, writer);
    }

    private void VisitEnumTypeExtension(
        EnumTypeExtensionNode node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Extend);
        writer.WriteSpace();
        VisitEnumTypeDefinitionBase(node, writer);
    }

    private void VisitEnumTypeDefinitionBase(
        EnumTypeDefinitionNodeBase node,
        ISyntaxWriter writer)
    {
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

    private void VisitInputObjectTypeDefinition(
        InputObjectTypeDefinitionNode node,
        ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);
        VisitInputObjectTypeDefinitionBase(node, writer);
    }

    private void VisitInputObjectTypeExtension(
        InputObjectTypeExtensionNode node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Extend);
        writer.WriteSpace();
        VisitInputObjectTypeDefinitionBase(node, writer);
    }

    private void VisitSchemaCoordinate(
        SchemaCoordinateNode node,
        ISyntaxWriter writer)
    {
        if (node.OfDirective)
        {
            writer.Write("@");
        }

        writer.WriteName(node.Name);

        if (node.MemberName is not null)
        {
            writer.Write(".");
            writer.WriteName(node.MemberName);
        }

        if (node.ArgumentName is not null)
        {
            writer.Write("(");
            writer.WriteName(node.ArgumentName);
            writer.Write(":)");
        }
    }

    private void VisitInputObjectTypeDefinitionBase(
        InputObjectTypeDefinitionNodeBase node,
        ISyntaxWriter writer)
    {
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

    private void VisitScalarTypeDefinition(
        ScalarTypeDefinitionNode node,
        ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);
        VisitScalarTypeDefinitionBase(node, writer);
    }

    private void VisitScalarTypeExtension(
        ScalarTypeExtensionNode node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Extend);
        writer.WriteSpace();
        VisitScalarTypeDefinitionBase(node, writer);
    }

    private void VisitScalarTypeDefinitionBase(
        NamedSyntaxNode node,
        ISyntaxWriter writer)
    {
        writer.Write(Keywords.Scalar);
        writer.WriteSpace();
        writer.WriteName(node.Name);

        WriteDirectives(node.Directives, writer);
    }

    private void VisitOperationTypeDefinition(
        OperationTypeDefinitionNode node,
        ISyntaxWriter writer)
    {
        writer.WriteIndent(_indented);

        writer.Write(node.Operation.ToString().ToLowerInvariant());
        writer.Write(": ");
        writer.WriteNamedType(node.Type);
    }

    private void VisitFieldDefinition(
        FieldDefinitionNode node,
        ISyntaxWriter writer)
    {
        writer.WriteIndent(_indented);

        WriteDescription(node.Description, writer);

        writer.WriteName(node.Name);

        if (node.Arguments.Count > 0)
        {
            WriteArgumentDefinitions(node.Arguments, writer);
        }

        writer.Write(":");
        writer.WriteSpace();

        writer.WriteType(node.Type);

        WriteDirectives(node.Directives, writer);
    }

    private void WriteArgumentDefinitions(
        IReadOnlyList<InputValueDefinitionNode> arguments,
        ISyntaxWriter writer)
    {
        if (!_indented)
        {
            writer.Write("(");
            writer.WriteMany(
                arguments,
                VisitArgumentValueDefinition,
                w => w.WriteSpace());
            writer.Write(")");
            return;
        }

        var flatWidth = MeasureFlatArgumentDefinitions(arguments);

        if (!ArgumentDefinitionsContainBlockString(arguments)
            && writer.Column + flatWidth <= _printWidth)
        {
            writer.Write("(");

            for (var i = 0; i < arguments.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(", ");
                }

                VisitArgumentValueDefinition(arguments[i], writer);
            }

            writer.Write(")");
        }
        else
        {
            writer.Write("(");
            writer.Indent();

            foreach (var argument in arguments)
            {
                writer.WriteLine();
                writer.WriteIndent();

                if (argument.Description is { })
                {
                    writer.WriteStringValue(argument.Description);
                    writer.WriteLine();
                    writer.WriteIndent();
                }

                WriteInputValueDefinition(argument, writer);
            }

            writer.WriteLine();
            writer.Unindent();
            writer.WriteIndent();
            writer.Write(")");
        }
    }

    private void VisitInputValueDefinition(
        InputValueDefinitionNode node,
        ISyntaxWriter writer)
    {
        writer.WriteIndent(_indented);

        WriteDescription(node.Description, writer);

        WriteInputValueDefinition(node, writer);
    }

    private void VisitDirectiveDefinition(
        DirectiveDefinitionNode node,
        ISyntaxWriter writer)
    {
        WriteDescription(node.Description, writer);

        writer.Write(Keywords.Directive);
        writer.WriteSpace();
        writer.Write('@');
        writer.WriteName(node.Name);

        if (node.Arguments.Count > 0)
        {
            WriteArgumentDefinitions(node.Arguments, writer);
        }

        writer.WriteSpace();

        if (node.IsRepeatable)
        {
            writer.Write(Keywords.Repeatable);
            writer.WriteSpace();
        }

        writer.Write(Keywords.On);

        WriteSeparatedList(
            node.Locations,
            " | ",
            "| ",
            SeparatedListBreakStyle.Leading,
            " ",
            (n, w) => w.WriteName(n),
            writer);
    }

    private void VisitArgumentValueDefinition(
        InputValueDefinitionNode node,
        ISyntaxWriter writer)
    {
        if (node.Description is { })
        {
            writer.WriteStringValue(node.Description);
            writer.WriteSpace();
        }

        WriteInputValueDefinition(node, writer);
    }

    private void WriteInputValueDefinition(
        InputValueDefinitionNode node,
        ISyntaxWriter writer)
    {
        writer.WriteName(node.Name);
        writer.Write(":");
        writer.WriteSpace();

        writer.WriteType(node.Type);

        if (node.DefaultValue is { } value)
        {
            writer.WriteSpace();
            writer.Write("=");
            writer.WriteSpace();
            WriteValue(value, writer);
        }

        if (node.Directives.Count > 0)
        {
            writer.WriteSpace();
            writer.WriteMany(
                node.Directives,
                (n, w) => w.WriteDirective(n),
                w => w.WriteSpace());
        }
    }

    private void VisitEnumValueDefinition(
        EnumValueDefinitionNode node,
        ISyntaxWriter writer)
    {
        writer.WriteIndent(_indented);

        WriteDescription(node.Description, writer);

        writer.WriteName(node.Name);

        WriteDirectives(node.Directives, writer);
    }

    private void WriteDescription(
        StringValueNode? description,
        ISyntaxWriter writer)
    {
        if (description is { })
        {
            writer.WriteStringValue(description);
            writer.WriteLine(_indented);
            writer.WriteSpace(!_indented);
            writer.WriteIndent(_indented);
        }
    }

    private void WriteDirectives(
        IReadOnlyList<DirectiveNode> directives,
        ISyntaxWriter writer)
    {
        if (directives.Count == 0)
        {
            return;
        }

        if (!_indented)
        {
            writer.WriteSpace();
            writer.WriteMany(
                directives,
                (n, w) => w.WriteDirective(n),
                w => w.WriteSpace());
            return;
        }

        if (_maxDirectivesPerLine < directives.Count)
        {
            writer.WriteLine();
            writer.Indent();
            writer.WriteIndent();
            writer.WriteMany(
                directives,
                (n, w) => WriteDirective(n, w),
                w =>
                {
                    w.WriteLine();
                    w.WriteIndent();
                });
            writer.Unindent();
            return;
        }

        var flatWidth = MeasureFlatDirectives(directives);

        if (!DirectivesContainBlockString(directives)
            && writer.Column + flatWidth <= _printWidth)
        {
            writer.WriteSpace();
            writer.WriteMany(
                directives,
                (n, w) => w.WriteDirective(n),
                w => w.WriteSpace());
        }
        else
        {
            writer.WriteLine();
            writer.Indent();
            writer.WriteIndent();
            writer.WriteMany(
                directives,
                (n, w) => WriteDirective(n, w),
                w =>
                {
                    w.WriteLine();
                    w.WriteIndent();
                });
            writer.Unindent();
        }
    }

    private int MeasureFlatArgumentDefinitions(
        IReadOnlyList<InputValueDefinitionNode> arguments)
    {
        var writer = StringSyntaxWriter.Rent();

        try
        {
            writer.Write("(");

            for (var i = 0; i < arguments.Count; i++)
            {
                if (i > 0)
                {
                    writer.Write(", ");
                }

                WriteFlatInputValueDefinition(arguments[i], writer);
            }

            writer.Write(")");
            return writer.Column;
        }
        finally
        {
            StringSyntaxWriter.Return(writer);
        }
    }

    private void WriteFlatInputValueDefinition(
        InputValueDefinitionNode node,
        ISyntaxWriter writer)
    {
        if (node.Description is { })
        {
            writer.WriteStringValue(node.Description);
            writer.WriteSpace();
        }

        writer.WriteName(node.Name);
        writer.Write(": ");
        writer.WriteType(node.Type);

        if (node.DefaultValue is { } value)
        {
            writer.Write(" = ");
            writer.WriteValue(value, false);
        }

        foreach (var directive in node.Directives)
        {
            writer.WriteSpace();
            writer.WriteDirective(directive);
        }
    }

    private int MeasureFlatDirectives(IReadOnlyList<DirectiveNode> directives)
    {
        var writer = StringSyntaxWriter.Rent();

        try
        {
            for (var i = 0; i < directives.Count; i++)
            {
                writer.WriteSpace();
                writer.WriteDirective(directives[i]);
            }

            return writer.Column;
        }
        finally
        {
            StringSyntaxWriter.Return(writer);
        }
    }

    private void WriteLeftBrace(ISyntaxWriter writer)
    {
        writer.WriteSpace();
        writer.Write("{");
        WriteLineOrSpace(writer);
    }

    private void WriteRightBrace(ISyntaxWriter writer)
    {
        WriteLineOrSpace(writer);
        writer.Write("}");
    }

    private void WriteLineOrSpace(ISyntaxWriter writer)
    {
        writer.WriteLine(_indented);
        writer.WriteSpace(!_indented);
    }
}
