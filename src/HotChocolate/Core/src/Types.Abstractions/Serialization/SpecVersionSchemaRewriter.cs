using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Serialization;

internal static class SpecVersionSchemaRewriter
{
    public static DocumentNode Rewrite(DocumentNode schema, GraphQLSpecVersion version)
    {
        var profile = GraphQLSpecVersionProfile.For(version);
        var removedDirectives = new HashSet<string>(StringComparer.Ordinal);
        var changedDirectiveLocations = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

        CollectDirectiveDefinitionChanges(schema, profile, removedDirectives, changedDirectiveLocations);

        var context = new RewriteContext(profile, removedDirectives, changedDirectiveLocations);
        var definitions = new List<IDefinitionNode>(schema.Definitions.Count);
        var rewritten = false;

        foreach (var definition in schema.Definitions)
        {
            if (ShouldRemoveDefinition(definition, removedDirectives))
            {
                rewritten = true;
                continue;
            }

            var rewrittenDefinition = RewriteDefinition(definition, context);
            rewritten |= !ReferenceEquals(definition, rewrittenDefinition);
            definitions.Add(rewrittenDefinition);
        }

        return rewritten ? schema.WithDefinitions(definitions) : schema;
    }

    private static void CollectDirectiveDefinitionChanges(
        DocumentNode schema,
        GraphQLSpecVersionProfile profile,
        HashSet<string> removedDirectives,
        Dictionary<string, IReadOnlySet<string>> changedDirectiveLocations)
    {
        foreach (var definition in schema.Definitions)
        {
            if (definition is not DirectiveDefinitionNode directiveDefinition
                || DirectiveNames.IsSpecDirective(directiveDefinition.Name.Value))
            {
                continue;
            }

            var locations = directiveDefinition.Locations
                .Where(t => profile.IsDirectiveLocationAllowed(t.Value))
                .Select(t => t.Value)
                .ToHashSet(StringComparer.Ordinal);

            if (locations.Count == directiveDefinition.Locations.Count)
            {
                continue;
            }

            if (locations.Count == 0)
            {
                removedDirectives.Add(directiveDefinition.Name.Value);
            }
            else
            {
                changedDirectiveLocations.Add(directiveDefinition.Name.Value, locations);
            }
        }
    }

    private static bool ShouldRemoveDefinition(
        IDefinitionNode definition,
        IReadOnlySet<string> removedDirectives)
        => definition switch
        {
            DirectiveExtensionNode => true,
            DirectiveDefinitionNode directiveDefinition
                => DirectiveNames.IsSpecDirective(directiveDefinition.Name.Value)
                    || removedDirectives.Contains(directiveDefinition.Name.Value),
            ScalarTypeDefinitionNode scalar => SpecScalarNames.IsSpecScalar(scalar.Name.Value),
            _ => false
        };

    private static IDefinitionNode RewriteDefinition(IDefinitionNode definition, RewriteContext context)
        => definition switch
        {
            SchemaDefinitionNode node => RewriteSchemaDefinition(node, context),
            SchemaExtensionNode node => RewriteSchemaExtension(node, context),
            ScalarTypeDefinitionNode node => RewriteScalarDefinition(node, context),
            ScalarTypeExtensionNode node => RewriteScalarExtension(node, context),
            ObjectTypeDefinitionNode node => RewriteObjectDefinition(node, context),
            ObjectTypeExtensionNode node => RewriteObjectExtension(node, context),
            InterfaceTypeDefinitionNode node => RewriteInterfaceDefinition(node, context),
            InterfaceTypeExtensionNode node => RewriteInterfaceExtension(node, context),
            UnionTypeDefinitionNode node => RewriteUnionDefinition(node, context),
            UnionTypeExtensionNode node => RewriteUnionExtension(node, context),
            EnumTypeDefinitionNode node => RewriteEnumDefinition(node, context),
            EnumTypeExtensionNode node => RewriteEnumExtension(node, context),
            InputObjectTypeDefinitionNode node => RewriteInputObjectDefinition(node, context),
            InputObjectTypeExtensionNode node => RewriteInputObjectExtension(node, context),
            DirectiveDefinitionNode node => RewriteDirectiveDefinition(node, context),
            _ => definition
        };

    private static SchemaDefinitionNode RewriteSchemaDefinition(SchemaDefinitionNode node, RewriteContext context)
        => RewriteDirectives(node.Directives, "SCHEMA", context, out var directives)
            ? node.WithDirectives(directives)
            : node;

    private static SchemaExtensionNode RewriteSchemaExtension(SchemaExtensionNode node, RewriteContext context)
        => RewriteDirectives(node.Directives, "SCHEMA", context, out var directives)
            ? node.WithDirectives(directives)
            : node;

    private static ScalarTypeDefinitionNode RewriteScalarDefinition(
        ScalarTypeDefinitionNode node,
        RewriteContext context)
        => RewriteDirectives(node.Directives, "SCALAR", context, out var directives)
            ? node.WithDirectives(directives)
            : node;

    private static ScalarTypeExtensionNode RewriteScalarExtension(
        ScalarTypeExtensionNode node,
        RewriteContext context)
        => RewriteDirectives(node.Directives, "SCALAR", context, out var directives)
            ? node.WithDirectives(directives)
            : node;

    private static ObjectTypeDefinitionNode RewriteObjectDefinition(
        ObjectTypeDefinitionNode node,
        RewriteContext context)
    {
        var directivesChanged = RewriteDirectives(node.Directives, "OBJECT", context, out var directives);
        var fieldsChanged = RewriteFields(node.Fields, context, out var fields);
        return fieldsChanged
            ? node.WithFields(fields).WithDirectives(directives)
            : directivesChanged
                ? node.WithDirectives(directives)
                : node;
    }

    private static ObjectTypeExtensionNode RewriteObjectExtension(
        ObjectTypeExtensionNode node,
        RewriteContext context)
    {
        var directivesChanged = RewriteDirectives(node.Directives, "OBJECT", context, out var directives);
        var fieldsChanged = RewriteFields(node.Fields, context, out var fields);
        return fieldsChanged
            ? node.WithFields(fields).WithDirectives(directives)
            : directivesChanged
                ? node.WithDirectives(directives)
                : node;
    }

    private static InterfaceTypeDefinitionNode RewriteInterfaceDefinition(
        InterfaceTypeDefinitionNode node,
        RewriteContext context)
    {
        var directivesChanged = RewriteDirectives(node.Directives, "INTERFACE", context, out var directives);
        var fieldsChanged = RewriteFields(node.Fields, context, out var fields);
        return fieldsChanged
            ? node.WithFields(fields).WithDirectives(directives)
            : directivesChanged
                ? node.WithDirectives(directives)
                : node;
    }

    private static InterfaceTypeExtensionNode RewriteInterfaceExtension(
        InterfaceTypeExtensionNode node,
        RewriteContext context)
    {
        var directivesChanged = RewriteDirectives(node.Directives, "INTERFACE", context, out var directives);
        var fieldsChanged = RewriteFields(node.Fields, context, out var fields);
        return fieldsChanged
            ? node.WithFields(fields).WithDirectives(directives)
            : directivesChanged
                ? node.WithDirectives(directives)
                : node;
    }

    private static UnionTypeDefinitionNode RewriteUnionDefinition(
        UnionTypeDefinitionNode node,
        RewriteContext context)
        => RewriteDirectives(node.Directives, "UNION", context, out var directives)
            ? node.WithDirectives(directives)
            : node;

    private static UnionTypeExtensionNode RewriteUnionExtension(
        UnionTypeExtensionNode node,
        RewriteContext context)
        => RewriteDirectives(node.Directives, "UNION", context, out var directives)
            ? node.WithDirectives(directives)
            : node;

    private static EnumTypeDefinitionNode RewriteEnumDefinition(
        EnumTypeDefinitionNode node,
        RewriteContext context)
    {
        var directivesChanged = RewriteDirectives(node.Directives, "ENUM", context, out var directives);
        var valuesChanged = RewriteEnumValues(node.Values, context, out var values);
        return valuesChanged
            ? node.WithValues(values).WithDirectives(directives)
            : directivesChanged
                ? node.WithDirectives(directives)
                : node;
    }

    private static EnumTypeExtensionNode RewriteEnumExtension(
        EnumTypeExtensionNode node,
        RewriteContext context)
    {
        var directivesChanged = RewriteDirectives(node.Directives, "ENUM", context, out var directives);
        var valuesChanged = RewriteEnumValues(node.Values, context, out var values);
        return valuesChanged
            ? node.WithValues(values).WithDirectives(directives)
            : directivesChanged
                ? node.WithDirectives(directives)
                : node;
    }

    private static InputObjectTypeDefinitionNode RewriteInputObjectDefinition(
        InputObjectTypeDefinitionNode node,
        RewriteContext context)
    {
        var directivesChanged = RewriteDirectives(node.Directives, "INPUT_OBJECT", context, out var directives);
        var fieldsChanged = RewriteInputValues(node.Fields, "INPUT_FIELD_DEFINITION", context, out var fields);
        return fieldsChanged
            ? node.WithFields(fields).WithDirectives(directives)
            : directivesChanged
                ? node.WithDirectives(directives)
                : node;
    }

    private static InputObjectTypeExtensionNode RewriteInputObjectExtension(
        InputObjectTypeExtensionNode node,
        RewriteContext context)
    {
        var directivesChanged = RewriteDirectives(node.Directives, "INPUT_OBJECT", context, out var directives);
        var fieldsChanged = RewriteInputValues(node.Fields, "INPUT_FIELD_DEFINITION", context, out var fields);
        return fieldsChanged
            ? node.WithFields(fields).WithDirectives(directives)
            : directivesChanged
                ? node.WithDirectives(directives)
                : node;
    }

    private static DirectiveDefinitionNode RewriteDirectiveDefinition(
        DirectiveDefinitionNode node,
        RewriteContext context)
    {
        var locationsChanged = false;
        var locations = new List<NameNode>(node.Locations.Count);

        foreach (var location in node.Locations)
        {
            if (context.Profile.IsDirectiveLocationAllowed(location.Value))
            {
                locations.Add(location);
            }
            else
            {
                locationsChanged = true;
            }
        }

        var argumentsChanged = RewriteInputValues(
            node.Arguments,
            "ARGUMENT_DEFINITION",
            context,
            out var arguments);
        var directivesChanged = node.Directives.Count > 0;

        if (!locationsChanged && !argumentsChanged && !directivesChanged)
        {
            return node;
        }

        return new DirectiveDefinitionNode(
            node.Location,
            node.Name,
            node.Description,
            node.IsRepeatable,
            arguments,
            [],
            locations);
    }

    private static bool RewriteFields(
        IReadOnlyList<FieldDefinitionNode> fields,
        RewriteContext context,
        out IReadOnlyList<FieldDefinitionNode> rewrittenFields)
    {
        List<FieldDefinitionNode>? rewritten = null;

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var rewrittenField = RewriteField(field, context);

            if (!ReferenceEquals(field, rewrittenField) && rewritten is null)
            {
                rewritten = new List<FieldDefinitionNode>(fields.Count);
                for (var j = 0; j < i; j++)
                {
                    rewritten.Add(fields[j]);
                }
            }

            rewritten?.Add(rewrittenField);
        }

        rewrittenFields = rewritten ?? fields;
        return rewritten is not null;
    }

    private static FieldDefinitionNode RewriteField(FieldDefinitionNode field, RewriteContext context)
    {
        var argumentsChanged = RewriteInputValues(
            field.Arguments,
            "ARGUMENT_DEFINITION",
            context,
            out var arguments);
        var directivesChanged = RewriteDirectives(field.Directives, "FIELD_DEFINITION", context, out var directives);

        return argumentsChanged
            ? field.WithArguments(arguments).WithDirectives(directives)
            : directivesChanged
                ? field.WithDirectives(directives)
                : field;
    }

    private static bool RewriteEnumValues(
        IReadOnlyList<EnumValueDefinitionNode> values,
        RewriteContext context,
        out IReadOnlyList<EnumValueDefinitionNode> rewrittenValues)
    {
        List<EnumValueDefinitionNode>? rewritten = null;

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            var rewrittenValue = RewriteDirectives(value.Directives, "ENUM_VALUE", context, out var directives)
                ? value.WithDirectives(directives)
                : value;

            if (!ReferenceEquals(value, rewrittenValue) && rewritten is null)
            {
                rewritten = new List<EnumValueDefinitionNode>(values.Count);
                for (var j = 0; j < i; j++)
                {
                    rewritten.Add(values[j]);
                }
            }

            rewritten?.Add(rewrittenValue);
        }

        rewrittenValues = rewritten ?? values;
        return rewritten is not null;
    }

    private static bool RewriteInputValues(
        IReadOnlyList<InputValueDefinitionNode> values,
        string location,
        RewriteContext context,
        out IReadOnlyList<InputValueDefinitionNode> rewrittenValues)
    {
        List<InputValueDefinitionNode>? rewritten = null;

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            var rewrittenValue = RewriteInputValue(value, location, context);

            if (!ReferenceEquals(value, rewrittenValue) && rewritten is null)
            {
                rewritten = new List<InputValueDefinitionNode>(values.Count);
                for (var j = 0; j < i; j++)
                {
                    rewritten.Add(values[j]);
                }
            }

            rewritten?.Add(rewrittenValue);
        }

        rewrittenValues = rewritten ?? values;
        return rewritten is not null;
    }

    private static InputValueDefinitionNode RewriteInputValue(
        InputValueDefinitionNode value,
        string location,
        RewriteContext context)
    {
        var deprecatedReason = default(string);
        var directivesChanged = RewriteDirectives(
            value.Directives,
            location,
            context,
            out var directives,
            out deprecatedReason);

        if (deprecatedReason is null)
        {
            return directivesChanged ? value.WithDirectives(directives) : value;
        }

        var description = value.Description is { } existing
            ? $"{existing.Value}\n\nDeprecated: {deprecatedReason}"
            : $"Deprecated: {deprecatedReason}";
        return new InputValueDefinitionNode(
            value.Location,
            value.Name,
            new StringValueNode(null, description, description.Contains('\n')),
            value.Type,
            value.DefaultValue,
            directives);
    }

    private static bool RewriteDirectives(
        IReadOnlyList<DirectiveNode> directives,
        string location,
        RewriteContext context,
        out IReadOnlyList<DirectiveNode> rewrittenDirectives)
        => RewriteDirectives(directives, location, context, out rewrittenDirectives, out _);

    private static bool RewriteDirectives(
        IReadOnlyList<DirectiveNode> directives,
        string location,
        RewriteContext context,
        out IReadOnlyList<DirectiveNode> rewrittenDirectives,
        out string? deprecatedReason)
    {
        deprecatedReason = null;
        List<DirectiveNode>? rewritten = null;

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];
            if (!ShouldRemoveDirective(directive.Name.Value, location, context))
            {
                rewritten?.Add(directive);
                continue;
            }

            rewritten ??= new List<DirectiveNode>(directives.Count - 1);
            for (var j = 0; j < i; j++)
            {
                rewritten.Add(directives[j]);
            }

            if (directive.Name.Value == DirectiveNames.Deprecated.Name
                && (location == "ARGUMENT_DEFINITION" || location == "INPUT_FIELD_DEFINITION"))
            {
                deprecatedReason = GetDeprecationReason(directive);
            }
        }

        rewrittenDirectives = rewritten ?? directives;
        return rewritten is not null;
    }

    private static bool ShouldRemoveDirective(string name, string location, RewriteContext context)
    {
        if (context.RemovedDirectives.Contains(name))
        {
            return true;
        }

        if (DirectiveNames.IsSpecDirective(name))
        {
            return !context.Profile.SpecDirectiveLocations.TryGetValue(name, out var specLocations)
                || !specLocations.Contains(location);
        }

        return context.ChangedDirectiveLocations.TryGetValue(name, out var locations)
            && !locations.Contains(location);
    }

    private static string GetDeprecationReason(DirectiveNode directive)
    {
        foreach (var argument in directive.Arguments)
        {
            if (argument.Name.Value == DirectiveNames.Deprecated.Arguments.Reason
                && argument.Value is StringValueNode reason)
            {
                return reason.Value;
            }
        }

        return DirectiveNames.Deprecated.Arguments.DefaultReason;
    }

    private sealed class RewriteContext(
        GraphQLSpecVersionProfile profile,
        HashSet<string> removedDirectives,
        Dictionary<string, IReadOnlySet<string>> changedDirectiveLocations)
    {
        public GraphQLSpecVersionProfile Profile { get; } = profile;

        public HashSet<string> RemovedDirectives { get; } = removedDirectives;

        public Dictionary<string, IReadOnlySet<string>> ChangedDirectiveLocations { get; } =
            changedDirectiveLocations;
    }
}
