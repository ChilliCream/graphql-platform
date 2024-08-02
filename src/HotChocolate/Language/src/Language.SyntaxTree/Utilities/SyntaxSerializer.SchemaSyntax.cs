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
            writer.WriteMany(node.Interfaces, (n, _) => writer.WriteNamedType(n), " & ");
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
            writer.WriteMany(
                node.Interfaces,
                (n, _) => writer.WriteNamedType(n),
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
        writer.WriteSpace();

        writer.WriteMany(node.Types, (n, _) => writer.WriteNamedType(n), " | ");
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

        writer.WriteMany(node.Locations, (n, _) => writer.WriteName(n), " | ");
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

        if (node.DefaultValue is { Kind: not SyntaxKind.NullValue, } value)
        {
            writer.WriteSpace();
            writer.Write("=");
            writer.WriteSpace();
            writer.WriteValue(value);
        }

        WriteDirectives(node.Directives, writer);
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
        if (_maxDirectivesPerLine < directives.Count)
        {
            writer.WriteLine();
            writer.Indent();
            writer.WriteIndent();
            writer.WriteMany(
                directives,
                (n, w) => w.WriteDirective(n),
                w =>
                {
                    w.WriteLine();
                    w.WriteIndent();
                });
            writer.Unindent();
        }
        else if (directives.Count > 0)
        {
            writer.WriteSpace();
            writer.WriteMany(
                directives,
                (n, w) => w.WriteDirective(n),
                w => w.WriteSpace());
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
