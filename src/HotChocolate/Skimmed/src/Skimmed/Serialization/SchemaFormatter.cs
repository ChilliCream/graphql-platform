using System.Reflection.Metadata.Ecma335;
using HotChocolate.Language;

namespace HotChocolate.Skimmed.Serialization;

public static class SchemaFormatter
{
    private static readonly SchemaFormatterVisitor _visitor = new();

    public static string FormatAsString(Schema schema, bool indented = true)
    {
        var context = new VisitorContext();
        _visitor.Visit(schema, context);
        return ((DocumentNode)context.Result!).ToString(indented);
    }

    private sealed class SchemaFormatterVisitor : SchemaVisitor<VisitorContext>
    {
        public override void Visit(Schema schema, VisitorContext context)
        {
            var definitions = new List<IDefinitionNode>();

            Visit(schema.Types, context);
            definitions.AddRange((List<IDefinitionNode>)context.Result!);

            context.Result = new DocumentNode(definitions);
        }

        public override void Visit(TypeCollection types, VisitorContext context)
        {
            var definitionNodes = new List<IDefinitionNode>();

            foreach (var type in types)
            {
                Visit(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            context.Result = definitionNodes;
        }

        public override void Visit(ObjectType type, VisitorContext context)
        {
            Visit(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            Visit(type.Fields, context);
            var fields = (List<FieldDefinitionNode>)context.Result!;

            context.Result =
                type.ContextData.ContainsKey(WellKnownContextData.TypeExtension)
                    ? new ObjectTypeExtensionNode(
                        null,
                        new NameNode(type.Name),
                        directives,
                        type.Implements.Select(t => new NamedTypeNode(t.Name)).ToList(),
                        fields)
                    : new ObjectTypeDefinitionNode(
                        null,
                        new NameNode(type.Name),
                        type.Description is not null
                            ? new StringValueNode(type.Description)
                            : null,
                        directives,
                        type.Implements.Select(t => new NamedTypeNode(t.Name)).ToList(),
                        fields);
        }

        public override void Visit(InterfaceType type, VisitorContext context)
        {
            Visit(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            Visit(type.Fields, context);
            var fields = (List<FieldDefinitionNode>)context.Result!;

            context.Result =
                type.ContextData.ContainsKey(WellKnownContextData.TypeExtension)
                    ? new ObjectTypeExtensionNode(
                        null,
                        new NameNode(type.Name),
                        directives,
                        type.Implements.Select(t => new NamedTypeNode(t.Name)).ToList(),
                        fields)
                    : new ObjectTypeDefinitionNode(
                        null,
                        new NameNode(type.Name),
                        type.Description is not null
                            ? new StringValueNode(type.Description)
                            : null,
                        directives,
                        type.Implements.Select(t => new NamedTypeNode(t.Name)).ToList(),
                        fields);
        }

        public override void Visit(InputObjectType type, VisitorContext context)
        {
            Visit(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            Visit(type.Fields, context);
            var fields = (List<InputValueDefinitionNode>)context.Result!;

            context.Result =
                type.ContextData.ContainsKey(WellKnownContextData.TypeExtension)
                    ? new InputObjectTypeExtensionNode(
                        null,
                        new NameNode(type.Name),
                        directives,
                        fields)
                    : new InputObjectTypeDefinitionNode(
                        null,
                        new NameNode(type.Name),
                        type.Description is not null
                            ? new StringValueNode(type.Description)
                            : null,
                        directives,
                        fields);
        }

        public override void Visit(ScalarType type, VisitorContext context)
        {
            Visit(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            context.Result =
                type.ContextData.ContainsKey(WellKnownContextData.TypeExtension)
                    ? new ScalarTypeExtensionNode(
                        null,
                        new NameNode(type.Name),
                        directives)
                    : new ScalarTypeDefinitionNode(
                        null,
                        new NameNode(type.Name),
                        type.Description is not null
                            ? new StringValueNode(type.Description)
                            : null,
                        directives);
        }

        public override void Visit(FieldCollection<OutputField> fields, VisitorContext context)
        {
            var fieldNodes = new List<FieldDefinitionNode>();

            foreach (var field in fields)
            {
                Visit(field, context);
                fieldNodes.Add((FieldDefinitionNode)context.Result!);
            }

            context.Result = fieldNodes;
        }

        public override void Visit(OutputField field, VisitorContext context)
        {
            Visit(field.Arguments, context);
            var arguments = (List<InputValueDefinitionNode>)context.Result!;

            Visit(field.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            context.Result = new FieldDefinitionNode(
                null,
                new NameNode(field.Name),
                field.Description is not null
                    ? new StringValueNode(field.Description)
                    : null,
                arguments,
                field.Type.ToTypeNode(),
                directives);
        }

        public override void Visit(FieldCollection<InputField> fields, VisitorContext context)
        {
            var inputNodes = new List<InputValueDefinitionNode>();

            foreach (var field in fields)
            {
                Visit(field, context);
                inputNodes.Add((InputValueDefinitionNode)context.Result!);
            }

            context.Result = inputNodes;
        }

        public override void Visit(InputField field, VisitorContext context)
        {
            Visit(field.Directives, context);

            context.Result = new InputValueDefinitionNode(
                null,
                new NameNode(field.Name),
                field.Description is not null
                    ? new StringValueNode(field.Description)
                    : null,
                field.Type.ToTypeNode(),
                field.DefaultValue,
                (List<DirectiveNode>)context.Result!);
        }

        public override void Visit(DirectiveCollection directives, VisitorContext context)
        {
            var directiveNodes = new List<DirectiveNode>();

            foreach (var directive in directives)
            {
                Visit(directive, context);
                directiveNodes.Add((DirectiveNode)context.Result!);
            }

            context.Result = directiveNodes;
        }

        public override void Visit(Directive directive, VisitorContext context)
        {
            Visit(directive.Arguments, context);
            context.Result = new DirectiveNode(
                null,
                new NameNode(directive.Name),
                (List<ArgumentNode>)context.Result!);
        }

        public override void Visit(IReadOnlyList<Argument> arguments, VisitorContext context)
        {
            var argumentNodes = new List<ArgumentNode>();

            foreach (var argument in arguments)
            {
                Visit(argument, context);
                argumentNodes.Add((ArgumentNode)context.Result!);
            }

            context.Result = argumentNodes;
        }

        public override void Visit(Argument argument, VisitorContext context)
        {
            context.Result = new ArgumentNode(argument.Name, argument.Value);
        }
    }

    private sealed class VisitorContext
    {
        public object? Result { get; set; }
    }
}
