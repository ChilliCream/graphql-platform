using HotChocolate.Language;

namespace HotChocolate.Skimmed.Serialization;

public static class SchemaFormatter
{
    private static readonly SchemaFormatterVisitor _visitor = new();

    public static string FormatAsString(Schema schema, bool indented = true)
    {
        var context = new VisitorContext();
        _visitor.VisitSchema(schema, context);
        return ((DocumentNode)context.Result!).ToString(indented);
    }

    private sealed class SchemaFormatterVisitor : SchemaVisitor<VisitorContext>
    {
        public override void VisitSchema(Schema schema, VisitorContext context)
        {
            var definitions = new List<IDefinitionNode>();

            VisitTypes(schema.Types, context);
            definitions.AddRange((List<IDefinitionNode>)context.Result!);

            VisitDirectiveTypes(schema.DirectiveTypes, context);
            definitions.AddRange((List<IDefinitionNode>)context.Result!);

            if (schema.QueryType is not null ||
                schema.MutationType is not null ||
                schema.SubscriptionType is not null)
            {
                var operationTypes = new List<OperationTypeDefinitionNode>();

                if (schema.QueryType is not null)
                {
                    operationTypes.Add(
                        new OperationTypeDefinitionNode(
                            null,
                            OperationType.Query,
                            new NamedTypeNode(schema.QueryType.Name)));
                }

                if (schema.MutationType is not null)
                {
                    operationTypes.Add(
                        new OperationTypeDefinitionNode(
                            null,
                            OperationType.Mutation,
                            new NamedTypeNode(schema.MutationType.Name)));
                }

                if (schema.SubscriptionType is not null)
                {
                    operationTypes.Add(
                        new OperationTypeDefinitionNode(
                            null,
                            OperationType.Subscription,
                            new NamedTypeNode(schema.SubscriptionType.Name)));
                }

                VisitDirectives(schema.Directives, context);

                var schemaDefinition = new SchemaDefinitionNode(
                    null,
                    string.IsNullOrEmpty(schema.Description)
                        ? null
                        : new(schema.Description),
                    (IReadOnlyList<DirectiveNode>)context.Result!,
                    operationTypes);
                definitions.Add(schemaDefinition);
            }


            context.Result = new DocumentNode(definitions);
        }

        public override void VisitTypes(TypeCollection types, VisitorContext context)
        {
            var definitionNodes = new List<IDefinitionNode>();

            foreach (var type in types.OrderBy(t => t.Name))
            {
                if (type is ScalarType { IsSpecScalar: true })
                {
                    continue;
                }

                VisitType(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            context.Result = definitionNodes;
        }

        public override void VisitDirectiveTypes(
            DirectiveTypeCollection directiveTypes,
            VisitorContext context)
        {
            var definitionNodes = new List<IDefinitionNode>();

            foreach (var type in directiveTypes.OrderBy(t => t.Name))
            {
                VisitDirectiveType(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            context.Result = definitionNodes;
        }

        public override void VisitObjectType(ObjectType type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            VisitOutputFields(type.Fields, context);
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

        public override void VisitInterfaceType(InterfaceType type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            VisitOutputFields(type.Fields, context);
            var fields = (List<FieldDefinitionNode>)context.Result!;

            context.Result =
                type.ContextData.ContainsKey(WellKnownContextData.TypeExtension)
                    ? new InterfaceTypeExtensionNode(
                        null,
                        new NameNode(type.Name),
                        directives,
                        type.Implements.Select(t => new NamedTypeNode(t.Name)).ToList(),
                        fields)
                    : new InterfaceTypeDefinitionNode(
                        null,
                        new NameNode(type.Name),
                        type.Description is not null
                            ? new StringValueNode(type.Description)
                            : null,
                        directives,
                        type.Implements.Select(t => new NamedTypeNode(t.Name)).ToList(),
                        fields);
        }

        public override void VisitInputObjectType(InputObjectType type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            VisitInputFields(type.Fields, context);
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

        public override void VisitScalarType(ScalarType type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
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

        public override void VisitUnionType(UnionType type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            context.Result =
                type.ContextData.ContainsKey(WellKnownContextData.TypeExtension)
                    ? new UnionTypeExtensionNode(
                        null,
                        new NameNode(type.Name),
                        directives,
                        type.Types.Select(t => new NamedTypeNode(t.Name)).ToList())
                    : new UnionTypeDefinitionNode(
                        null,
                        new NameNode(type.Name),
                        type.Description is not null
                            ? new StringValueNode(type.Description)
                            : null,
                        directives,
                        type.Types.Select(t => new NamedTypeNode(t.Name)).ToList());
        }

        public override void VisitDirectiveType(
            DirectiveType directive,
            VisitorContext context)
        {
            VisitInputFields(directive.Arguments, context);
            var arguments = (List<InputValueDefinitionNode>)context.Result!;

            context.Result =
                new DirectiveDefinitionNode(
                    null,
                    new NameNode(directive.Name),
                    directive.Description is not null
                        ? new StringValueNode(directive.Description)
                        : null,
                    directive.IsRepeatable,
                    arguments,
                    directive.Locations.ToNameNodes());
        }

        public override void VisitOutputFields(
            FieldCollection<OutputField> fields,
            VisitorContext context)
        {
            var fieldNodes = new List<FieldDefinitionNode>();

            foreach (var field in fields.OrderBy(t => t.Name))
            {
                VisitOutputField(field, context);
                fieldNodes.Add((FieldDefinitionNode)context.Result!);
            }

            context.Result = fieldNodes;
        }

        public override void VisitOutputField(OutputField field, VisitorContext context)
        {
            VisitInputFields(field.Arguments, context);
            var arguments = (List<InputValueDefinitionNode>)context.Result!;

            VisitDirectives(field.Directives, context);
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

        public override void VisitInputFields(
            FieldCollection<InputField> fields,
            VisitorContext context)
        {
            var inputNodes = new List<InputValueDefinitionNode>();

            foreach (var field in fields.OrderBy(t => t.Name))
            {
                VisitInputField(field, context);
                inputNodes.Add((InputValueDefinitionNode)context.Result!);
            }

            context.Result = inputNodes;
        }

        public override void VisitInputField(InputField field, VisitorContext context)
        {
            VisitDirectives(field.Directives, context);

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

        public override void VisitDirectives(DirectiveCollection directives, VisitorContext context)
        {
            var directiveNodes = new List<DirectiveNode>();

            foreach (var directive in directives)
            {
                VisitDirective(directive, context);
                directiveNodes.Add((DirectiveNode)context.Result!);
            }

            context.Result = directiveNodes;
        }

        public override void VisitDirective(Directive directive, VisitorContext context)
        {
            VisitArguments(directive.Arguments, context);
            context.Result = new DirectiveNode(
                null,
                new NameNode(directive.Name),
                (List<ArgumentNode>)context.Result!);
        }

        public override void VisitArguments(
            ArgumentCollection arguments,
            VisitorContext context)
        {
            var argumentNodes = new List<ArgumentNode>();

            foreach (var argument in arguments)
            {
                VisitArgument(argument, context);
                argumentNodes.Add((ArgumentNode)context.Result!);
            }

            context.Result = argumentNodes;
        }

        public override void VisitArgument(Argument argument, VisitorContext context)
        {
            context.Result = new ArgumentNode(argument.Name, argument.Value);
        }
    }

    private sealed class VisitorContext
    {
        public object? Result { get; set; }
    }
}
