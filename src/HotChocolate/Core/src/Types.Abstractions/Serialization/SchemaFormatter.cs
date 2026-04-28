using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Serialization;

public static class SchemaFormatter
{
    private static readonly SchemaFormatterVisitor s_visitor = new();
    private static readonly SyntaxSerializerOptions s_options =
        new()
        {
            Indented = true,
            PrintWidth = 80
        };

    public static string FormatAsString(
        ISchemaDefinition schema,
        SchemaFormatterOptions options = default)
    {
        var document = FormatAsDocument(schema, options);

        if (options.Indented == false)
        {
            return document.ToString(false);
        }

        return document.ToString(s_options);
    }

    public static DocumentNode FormatAsDocument(
        ISchemaDefinition schema,
        SchemaFormatterOptions options = default)
    {
        var context = new VisitorContext
        {
            Schema = schema,
            OrderTypesByName = options.OrderTypesByName ?? options.OrderByName ?? true,
            OrderFieldsByName = options.OrderFieldsByName ?? options.OrderByName ?? true,
            PrintSpecScalars = options.PrintSpecScalars ?? false,
            PrintSpecDirectives = options.PrintSpecDirectives ?? false
        };
        s_visitor.VisitSchema(schema, context);
        return (DocumentNode)context.Result!;
    }

    private sealed class SchemaFormatterVisitor : SchemaDefinitionVisitor<VisitorContext>
    {
        public override void VisitSchema(ISchemaDefinition schema, VisitorContext context)
        {
            if (!ReferenceEquals(context.Schema, schema))
            {
                throw new InvalidOperationException(
                    "The schema must be the same as the schema on the context.");
            }

            var definitions = new List<IDefinitionNode>();

            var hasQuery = schema.TryGetOperationType(OperationType.Query, out var queryType);
            var hasMutation = schema.TryGetOperationType(OperationType.Mutation, out var mutationType);
            var hasSubscription = schema.TryGetOperationType(OperationType.Subscription, out var subscriptionType);

            if (hasQuery || hasMutation || hasSubscription || !string.IsNullOrEmpty(schema.Description))
            {
                var operationTypes = new List<OperationTypeDefinitionNode>();

                if (hasQuery)
                {
                    operationTypes.Add(
                        new OperationTypeDefinitionNode(
                            null,
                            OperationType.Query,
                            new NamedTypeNode(queryType!.Name)));
                }

                if (hasMutation)
                {
                    operationTypes.Add(
                        new OperationTypeDefinitionNode(
                            null,
                            OperationType.Mutation,
                            new NamedTypeNode(mutationType!.Name)));
                }

                if (hasSubscription)
                {
                    operationTypes.Add(
                        new OperationTypeDefinitionNode(
                            null,
                            OperationType.Subscription,
                            new NamedTypeNode(subscriptionType!.Name)));
                }

                VisitDirectives(schema.Directives, context);

                var schemaDefinition = new SchemaDefinitionNode(
                    null,
                    CreateDescription(schema.Description),
                    (IReadOnlyList<DirectiveNode>)context.Result!,
                    operationTypes);
                definitions.Add(schemaDefinition);
            }

            if (context.OrderTypesByName)
            {
                VisitTypes(schema.Types, context);
                definitions.AddRange((List<IDefinitionNode>)context.Result!);

                VisitDirectiveDefinitions(schema.DirectiveDefinitions, context);
                definitions.AddRange((List<IDefinitionNode>)context.Result!);
            }
            else
            {
                VisitTypesAndDirectives(schema, context);
                definitions.AddRange((List<IDefinitionNode>)context.Result!);
            }

            context.Result = new DocumentNode(definitions);
        }

        private void VisitTypesAndDirectives(ISchemaDefinition schema, VisitorContext context)
        {
            var definitionNodes = new List<IDefinitionNode>();

            foreach (var definition in schema.GetAllDefinitions())
            {
                if (definition is IDirectiveDefinition directiveDefinition)
                {
                    if (!context.PrintSpecDirectives
                        && DirectiveNames.IsSpecDirective(directiveDefinition.Name))
                    {
                        continue;
                    }

                    VisitDirectiveDefinition(directiveDefinition, context);
                    definitionNodes.Add((IDefinitionNode)context.Result!);
                }

                if (definition is ITypeDefinition namedTypeDefinition)
                {
                    if (!context.PrintSpecScalars
                        && namedTypeDefinition is IScalarTypeDefinition scalarType
                        && SpecScalarNames.IsSpecScalar(scalarType.Name))
                    {
                        continue;
                    }

                    VisitType(namedTypeDefinition, context);
                    definitionNodes.Add((IDefinitionNode)context.Result!);
                }
            }

            context.Result = definitionNodes;
        }

        public override void VisitTypes(
            IReadOnlyTypeDefinitionCollection typesDefinition,
            VisitorContext context)
        {
            var definitionNodes = new List<IDefinitionNode>();

            context.Schema.TryGetOperationType(OperationType.Query, out var queryType);
            context.Schema.TryGetOperationType(OperationType.Mutation, out var mutationType);
            context.Schema.TryGetOperationType(OperationType.Subscription, out var subscriptionType);

            if (queryType is not null)
            {
                VisitType(queryType, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            if (mutationType is not null)
            {
                VisitType(mutationType, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            if (subscriptionType is not null)
            {
                VisitType(subscriptionType, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            foreach (var type in typesDefinition
                .OfType<IObjectTypeDefinition>()
                .Where(t => !t.IsIntrospectionType)
                .OrderBy(t => t.Name))
            {
                if (queryType == type
                    || mutationType == type
                    || subscriptionType == type)
                {
                    continue;
                }

                VisitType(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            foreach (var type in typesDefinition
                .OfType<IInterfaceTypeDefinition>()
                .Where(t => !t.IsIntrospectionType)
                .OrderBy(t => t.Name))
            {
                VisitType(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            foreach (var type in typesDefinition
                .OfType<IUnionTypeDefinition>()
                .Where(t => !t.IsIntrospectionType)
                .OrderBy(t => t.Name))
            {
                VisitType(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            foreach (var type in typesDefinition
                .OfType<IInputObjectTypeDefinition>()
                .Where(t => !t.IsIntrospectionType)
                .OrderBy(t => t.Name))
            {
                VisitType(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            foreach (var type in typesDefinition
                .OfType<IEnumTypeDefinition>()
                .Where(t => !t.IsIntrospectionType)
                .OrderBy(t => t.Name))
            {
                VisitType(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            foreach (var type in typesDefinition
                .OfType<IScalarTypeDefinition>()
                .Where(t => !t.IsIntrospectionType)
                .OrderBy(t => t.Name))
            {
                if (!context.PrintSpecScalars
                    && SpecScalarNames.IsSpecScalar(type.Name))
                {
                    continue;
                }

                VisitType(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            context.Result = definitionNodes;
        }

        public override void VisitDirectiveDefinitions(
            IReadOnlyDirectiveDefinitionCollection directiveTypes,
            VisitorContext context)
        {
            var definitionNodes = new List<IDefinitionNode>();

            foreach (var type in directiveTypes
                .OrderBy(t => t.Name, context.OrderTypesByName))
            {
                if (DirectiveNames.IsSpecDirective(type.Name))
                {
                    continue;
                }

                VisitDirectiveDefinition(type, context);
                definitionNodes.Add((IDefinitionNode)context.Result!);
            }

            context.Result = definitionNodes;
        }

        public override void VisitObjectType(IObjectTypeDefinition type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            VisitOutputFields(type.Fields, context);
            var fields = (List<FieldDefinitionNode>)context.Result!;

            var interfaces = type.Implements.Select(t => new NamedTypeNode(t.Name)).ToList();

            context.Result = IsTypeExtension(type)
                ? new ObjectTypeExtensionNode(
                    null,
                    new NameNode(type.Name),
                    directives,
                    interfaces,
                    fields)
                : new ObjectTypeDefinitionNode(
                    null,
                    new NameNode(type.Name),
                    CreateDescription(type.Description),
                    directives,
                    interfaces,
                    fields);
        }

        public override void VisitInterfaceType(IInterfaceTypeDefinition type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            VisitOutputFields(type.Fields, context);
            var fields = (List<FieldDefinitionNode>)context.Result!;

            var interfaces = type.Implements.AsEnumerable().Select(t => new NamedTypeNode(t.Name)).ToList();

            context.Result = IsTypeExtension(type)
                ? new InterfaceTypeExtensionNode(
                    null,
                    new NameNode(type.Name),
                    directives,
                    interfaces,
                    fields)
                : new InterfaceTypeDefinitionNode(
                    null,
                    new NameNode(type.Name),
                    CreateDescription(type.Description),
                    directives,
                    interfaces,
                    fields);
        }

        public override void VisitInputObjectType(IInputObjectTypeDefinition type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            VisitInputFields(type.Fields, context);
            var fields = (List<InputValueDefinitionNode>)context.Result!;

            context.Result = IsTypeExtension(type)
                ? new InputObjectTypeExtensionNode(
                    null,
                    new NameNode(type.Name),
                    directives,
                    fields)
                : new InputObjectTypeDefinitionNode(
                    null,
                    new NameNode(type.Name),
                    CreateDescription(type.Description),
                    directives,
                    fields);
        }

        public override void VisitScalarType(IScalarTypeDefinition type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            if (type.SpecifiedBy is not null
                && !directives.Any(d => d.Name.Value == DirectiveNames.SpecifiedBy.Name))
            {
                directives.Add(
                    new DirectiveNode(
                        DirectiveNames.SpecifiedBy.Name,
                        new ArgumentNode(
                            DirectiveNames.SpecifiedBy.Arguments.Url,
                            new StringValueNode(type.SpecifiedBy.ToString()))));
            }

            context.Result = IsTypeExtension(type)
                ? new ScalarTypeExtensionNode(
                    null,
                    new NameNode(type.Name),
                    directives)
                : new ScalarTypeDefinitionNode(
                    null,
                    new NameNode(type.Name),
                    CreateDescription(type.Description),
                    directives);
        }

        public override void VisitEnumType(IEnumTypeDefinition type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            VisitEnumValues(type.Values, context);
            var values = (List<EnumValueDefinitionNode>)context.Result!;

            context.Result = IsTypeExtension(type)
                ? new EnumTypeExtensionNode(
                    null,
                    new NameNode(type.Name),
                    directives,
                    values)
                : new EnumTypeDefinitionNode(
                    null,
                    new NameNode(type.Name),
                    CreateDescription(type.Description),
                    directives,
                    values);
        }

        public override void VisitEnumValues(IReadOnlyEnumValueCollection values, VisitorContext context)
        {
            var definitionNodes = new List<EnumValueDefinitionNode>();

            foreach (var value in values.AsEnumerable().OrderBy(t => t.Name, context.OrderFieldsByName))
            {
                VisitEnumValue(value, context);
                definitionNodes.Add((EnumValueDefinitionNode)context.Result!);
            }

            context.Result = definitionNodes;
        }

        public override void VisitEnumValue(IEnumValue value, VisitorContext context)
        {
            VisitDirectives(value.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            directives = ApplyDeprecatedDirective(value, directives);

            context.Result = new EnumValueDefinitionNode(
                null,
                new NameNode(value.Name),
                CreateDescription(value.Description),
                directives);
        }

        public override void VisitUnionType(IUnionTypeDefinition type, VisitorContext context)
        {
            VisitDirectives(type.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            var memberTypes = type.Types.AsEnumerable().Select(t => new NamedTypeNode(t.Name)).ToList();

            context.Result = IsTypeExtension(type)
                ? new UnionTypeExtensionNode(
                    null,
                    new NameNode(type.Name),
                    directives,
                    memberTypes)
                : new UnionTypeDefinitionNode(
                    null,
                    new NameNode(type.Name),
                    CreateDescription(type.Description),
                    directives,
                    memberTypes);
        }

        public override void VisitDirectiveDefinition(
            IDirectiveDefinition mutableDirective,
            VisitorContext context)
        {
            VisitInputFields(mutableDirective.Arguments, context);
            var arguments = (List<InputValueDefinitionNode>)context.Result!;

            context.Result =
                new DirectiveDefinitionNode(
                    null,
                    new NameNode(mutableDirective.Name),
                    CreateDescription(mutableDirective.Description),
                    mutableDirective.IsRepeatable,
                    arguments,
                    mutableDirective.Locations.ToNameNodes());
        }

        public override void VisitOutputFields(
            IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> fields,
            VisitorContext context)
        {
            var fieldNodes = new List<FieldDefinitionNode>();

            foreach (var field in fields.AsEnumerable().OrderBy(t => t.Name, context.OrderFieldsByName))
            {
                if (field.IsIntrospectionField)
                {
                    continue;
                }

                VisitOutputField(field, context);
                fieldNodes.Add((FieldDefinitionNode)context.Result!);
            }

            context.Result = fieldNodes;
        }

        public override void VisitOutputField(IOutputFieldDefinition field, VisitorContext context)
        {
            VisitInputFields(field.Arguments, context);
            var arguments = (List<InputValueDefinitionNode>)context.Result!;

            VisitDirectives(field.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            directives = ApplyDeprecatedDirective(field, directives);

            context.Result = new FieldDefinitionNode(
                null,
                new NameNode(field.Name),
                CreateDescription(field.Description),
                arguments,
                field.Type.ToTypeNode(),
                directives);
        }

        public override void VisitInputFields(
            IReadOnlyFieldDefinitionCollection<IInputValueDefinition> fields,
            VisitorContext context)
        {
            var inputNodes = new List<InputValueDefinitionNode>();

            foreach (var field in fields.AsEnumerable().OrderBy(t => t.Name, context.OrderFieldsByName))
            {
                VisitInputField(field, context);
                inputNodes.Add((InputValueDefinitionNode)context.Result!);
            }

            context.Result = inputNodes;
        }

        public override void VisitInputField(IInputValueDefinition field, VisitorContext context)
        {
            VisitDirectives(field.Directives, context);
            var directives = (List<DirectiveNode>)context.Result!;

            directives = ApplyDeprecatedDirective(field, directives);

            context.Result = new InputValueDefinitionNode(
                null,
                new NameNode(field.Name),
                CreateDescription(field.Description),
                field.Type.ToTypeNode(),
                field.DefaultValue,
                directives);
        }

        public override void VisitDirectives(IReadOnlyDirectiveCollection directives, VisitorContext context)
        {
            var directiveNodes = new List<DirectiveNode>();

            foreach (var directive in directives)
            {
                VisitDirective(directive, context);
                directiveNodes.Add((DirectiveNode)context.Result!);
            }

            context.Result = directiveNodes;
        }

        public override void VisitDirective(IDirective directive, VisitorContext context)
        {
            context.Result = directive.ToSyntaxNode();
        }

        private static List<DirectiveNode> ApplyDeprecatedDirective(
            IDeprecationProvider canBeDeprecated,
            List<DirectiveNode> directives)
        {
            if (canBeDeprecated.IsDeprecated)
            {
                var deprecateDirective = CreateDeprecatedDirective(canBeDeprecated.DeprecationReason);

                if (directives.Count == 0)
                {
                    directives = [deprecateDirective];
                }
                else
                {
                    var temp = directives.ToList();
                    temp.Add(deprecateDirective);
                    directives = temp;
                }
            }

            return directives;
        }

        private static DirectiveNode CreateDeprecatedDirective(string? reason = null)
        {
            if (string.IsNullOrEmpty(reason))
            {
                reason = DirectiveNames.Deprecated.Arguments.DefaultReason;
            }

            return new DirectiveNode(
                new NameNode(DirectiveNames.Deprecated.Name),
                [new ArgumentNode(DirectiveNames.Deprecated.Arguments.Reason, reason)]);
        }

        private static StringValueNode? CreateDescription(string? description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return null;
            }

            // Get rid of any unnecessary whitespace.
            description = description.Trim();

            var isBlock = description.Contains('\n');

            return new StringValueNode(null, description, isBlock);
        }

        private static bool IsTypeExtension(IFeatureProvider type)
            => type.Features.Get<TypeExtensionMarker>() is not null;
    }

    private sealed record VisitorContext
    {
        public required ISchemaDefinition Schema { get; init; }

        public required bool OrderTypesByName { get; init; }

        public required bool OrderFieldsByName { get; init; }

        public required bool PrintSpecScalars { get; init; }

        public required bool PrintSpecDirectives { get; init; }

        public object? Result { get; set; }
    }
}

file static class Extensions
{
    public static ITypeNode ToTypeNode(this IType type)
        => type switch
        {
            ITypeDefinition namedType => new NamedTypeNode(namedType.Name),
            ListType listType => new ListTypeNode(ToTypeNode(listType.ElementType)),
            NonNullType nonNullType => new NonNullTypeNode((INullableTypeNode)ToTypeNode(nonNullType.NullableType)),
            _ => throw new NotSupportedException()
        };

    public static IEnumerable<T> OrderBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        bool ifTrue)
        where TKey : IComparable<TKey>
        => ifTrue ? source.OrderBy(keySelector) : source;
}
