using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

internal static class TreeNodeExtensions
{
    public static IHasTreeNodes AddErrorMessage(
        this IHasTreeNodes node,
        string message)
    {
        return node.AddNode($"[red]{message.EscapeMarkup()}[/]");
    }

    public static IHasTreeNodes AddSchemaVersionChangeViolations(
        this IHasTreeNodes node,
        ISchemaVersionChangeViolationError error)
    {
        return node.AddSchemaChanges(error.Changes.OfType<ISchemaChange>());
    }

    public static IHasTreeNodes AddGraphQLSchemaErrors(
        this IHasTreeNodes node,
        IInvalidGraphQLSchemaError error)
    {
        foreach (var query in error.Errors)
        {
            node.AddNode($"[red]{query.Message.EscapeMarkup()}[/] [grey]{query.Code}[/]");
        }

        return node;
    }

    public static IHasTreeNodes AddPersistedQueryValidationErrors(
        this IHasTreeNodes node,
        IPersistedQueryValidationError error)
    {
        var client = error.Client;
        var clientNode = node.AddNode($"Client '{client?.Name.EscapeMarkup()}' (ID: {client?.Id})");

        foreach (var operation in error.Queries)
        {
            var publishingInfo = operation.DeployedTags.Count > 0
                ? $" (Deployed tags: {string.Join(",", operation.DeployedTags)})"
                : "";

            var operationNode = clientNode.AddNode($"Operation '{operation.Hash}'{publishingInfo}");

            foreach (var err in operation.Errors)
            {
                var errorLocation = string.Empty;
                if (err.Locations is { Count: > 0 } locations)
                {
                    errorLocation = $"({locations[0].Line}:{locations[0].Column})";
                }

                operationNode.AddNode($"{err.Message.EscapeMarkup()} {errorLocation}");
            }
        }

        return node;
    }

    public static IHasTreeNodes AddOpenApiCollectionValidationErrors(
        this IHasTreeNodes node,
        IOpenApiCollectionValidationError error)
    {
        foreach (var collectionError in error.Collections)
        {
            var openApiCollection = collectionError.OpenApiCollection;
            var collectionNode = node.AddNode(
                $"OpenAPI collection '{openApiCollection?.Name.EscapeMarkup()}' (ID: {openApiCollection?.Id})");

            foreach (var entity in collectionError.Entities)
            {
                var entityNode = collectionNode.AddNode(GetOpenApiEntityNodeHeading(entity));

                foreach (var entityError in entity.Errors)
                {
                    if (entityError is IOpenApiCollectionValidationDocumentError documentError)
                    {
                        var errorLocation = string.Empty;
                        if (documentError.Locations is { Count: > 0 } locations)
                        {
                            errorLocation = $"({locations[0].Line}:{locations[0].Column})";
                        }

                        entityNode.AddNode($"{documentError.Message.EscapeMarkup()} {errorLocation}");
                    }
                    else if (entityError is IOpenApiCollectionValidationEntityValidationError entityValidationError)
                    {
                        entityNode.AddNode(entityValidationError.Message.EscapeMarkup());
                    }
                    else
                    {
                        entityNode.AddNode(ErrorMessages.UnknownServerResponse);
                    }
                }
            }
        }

        return node;

        static string GetOpenApiEntityNodeHeading(IOpenApiCollectionValidationEntity entity)
        {
            return entity switch
            {
                IOpenApiCollectionValidationEndpoint endpoint => $"Endpoint '{endpoint.HttpMethod} {endpoint.Route}'",
                IOpenApiCollectionValidationModel model => $"Model '{model.Name}'",
                _ => "Unknown entity type"
            };
        }
    }

    public static IHasTreeNodes AddMcpFeatureCollectionValidationErrors(
        this IHasTreeNodes node,
        IMcpFeatureCollectionValidationError error)
    {
        foreach (var collectionError in error.Collections)
        {
            var mcpFeatureCollection = collectionError.McpFeatureCollection;
            var collectionNode = node.AddNode(
                $"MCP Feature Collection '{mcpFeatureCollection?.Name.EscapeMarkup()}' (ID: {mcpFeatureCollection?.Id})");

            foreach (var entity in collectionError.Entities)
            {
                var entityNode = collectionNode.AddNode(GetMcpEntityNodeHeading(entity));

                foreach (var entityError in entity.Errors)
                {
                    if (entityError is IMcpFeatureCollectionValidationDocumentError documentError)
                    {
                        var errorLocation = string.Empty;
                        if (documentError.Locations is { Count: > 0 } locations)
                        {
                            errorLocation = $"({locations[0].Line}:{locations[0].Column})";
                        }

                        entityNode.AddNode($"{documentError.Message.EscapeMarkup()} {errorLocation}");
                    }
                    else if (entityError is IMcpFeatureCollectionValidationEntityValidationError entityValidationError)
                    {
                        entityNode.AddNode(entityValidationError.Message.EscapeMarkup());
                    }
                    else
                    {
                        entityNode.AddNode(ErrorMessages.UnknownServerResponse);
                    }
                }
            }
        }

        return node;

        static string GetMcpEntityNodeHeading(IMcpFeatureCollectionValidationEntity entity)
        {
            return entity switch
            {
                IMcpFeatureCollectionValidationPrompt prompt => $"Prompt '{prompt.Name}'",
                IMcpFeatureCollectionValidationTool tool => $"Tool '{tool.Name}'",
                _ => "Unknown entity type"
            };
        }
    }

    public static IHasTreeNodes AddSchemaChanges(
        this IHasTreeNodes node,
        IEnumerable<ISchemaChange> changes)
    {
        foreach (var change in changes)
        {
            node.AddSchemaChange(change);
        }

        return node;
    }

    private static IHasTreeNodes AddSchemaChange(
        this IHasTreeNodes node,
        ISchemaChange change)
    {
        return change switch
        {
            IArgumentAdded c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"The argument {c.Coordinate.AsSchemaCoordinate()} was added"),

            IArgumentChanged c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"The argument {c.Coordinate.AsSchemaCoordinate()} has changed")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IArgumentRemoved c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"The argument {c.Coordinate.AsSchemaCoordinate()} was removed"),

            IDeprecatedChange { DeprecationReason: { } reason } c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"The member was deprecated with the reason {reason.EscapeMarkup()}"),

            IDeprecatedChange c => node
                .AddNodeWithSeverity(c.Severity, "The member was deprecated"),

            IDescriptionChanged { Old: { } old, New: { } @new } c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Description changed from {old.AsDescription()} to {@new.AsDescription()}"),

            IDescriptionChanged { New: { } @new } c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Description added: {@new.AsDescription()}"),

            IDescriptionChanged { Old: { } old } c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Description remove: {old.AsDescription()}"),

            IDirectiveLocationAdded c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Directive location {c.Location.ToString().AsSyntax()} added"),

            IDirectiveLocationRemoved c =>
                node.AddNodeWithSeverity(
                    c.Severity,
                    $"Directive location {c.Location.ToString().AsSyntax()} removed"),

            IDirectiveModifiedChange c =>
                node.AddNodeWithSeverity(
                        c.Severity,
                        $"Directive {c.Coordinate.AsSchemaCoordinate()} was modified")
                    .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IEnumModifiedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Enum {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IEnumValueAdded c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Enum value {c.Coordinate.AsSchemaCoordinate()} was added"),

            IEnumValueChanged c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Enum value {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IEnumValueRemoved c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Enum value {c.Coordinate.AsSchemaCoordinate()} was removed"),

            IFieldAddedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Field {c.Coordinate.AsSchemaCoordinate()} of type {c.TypeName.AsSyntax()} was added"),

            IFieldRemovedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Field {c.Coordinate.AsSchemaCoordinate()} of type {c.TypeName.AsSyntax()} was removed"),

            IInputFieldChanged c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Field {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IInputObjectModifiedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Field {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IInterfaceImplementationAdded c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Interface implementation {c.InterfaceName.AsSchemaCoordinate()} was added"),

            IInterfaceImplementationRemoved c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Interface implementation {c.InterfaceName.AsSchemaCoordinate()} was removed"),

            IInterfaceModifiedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Interface type {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IObjectModifiedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Object type {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IOutputFieldChanged c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Field {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            IPossibleTypeAdded c => node
                .AddNodeWithSeverity(c.Severity, $"Possible type {c.TypeName} added."),

            IPossibleTypeRemoved c => node
                .AddNodeWithSeverity(c.Severity, $"Possible type {c.TypeName} removed."),

            IScalarModifiedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Scalar {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            ITypeChanged c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Type changed from {c.OldType.AsSyntax()} to {c.NewType.AsSyntax()}"),

            ITypeSystemMemberAddedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Type system member {c.Coordinate.AsSchemaCoordinate()} was added."),

            ITypeSystemMemberRemovedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Type system member {c.Coordinate.AsSchemaCoordinate()} was removed."),

            IUnionMemberAdded c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Type {c.TypeName.AsSchemaCoordinate()} was added to the union."),

            IUnionMemberRemoved c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Type {c.TypeName.AsSchemaCoordinate()} was removed from the union."),

            IUnionModifiedChange c => node
                .AddNodeWithSeverity(
                    c.Severity,
                    $"Union {c.Coordinate.AsSchemaCoordinate()} was modified")
                .AddSchemaChanges(c.Changes.OfType<ISchemaChange>()),

            _ => node.AddNodeWithSeverity(
                SchemaChangeSeverity.Dangerous,
                "Unknown change. Try to update the version of ChilliCream.Nitro.CommandLine")
        };
    }

    private static IHasTreeNodes AddNodeWithSeverity(
        this IHasTreeNodes node,
        SchemaChangeSeverity severity,
        string message)
    {
        return severity switch
        {
            SchemaChangeSeverity.Breaking => node.AddBreakingNode(message),
            SchemaChangeSeverity.Dangerous => node.AddDangerousNode(message),
            SchemaChangeSeverity.Safe => node.AddSafeNode(message),
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
        };
    }

    private static IHasTreeNodes AddSafeNode(this IHasTreeNodes node, string message)
    {
        return node.AddNode($"[green]{Glyphs.Check} {message}[/]");
    }

    private static IHasTreeNodes AddDangerousNode(this IHasTreeNodes node, string message)
    {
        return node.AddNode($"[yellow]{Glyphs.ExclamationMark} {message}[/]");
    }

    private static IHasTreeNodes AddBreakingNode(this IHasTreeNodes node, string message)
    {
        return node.AddNode($"[red]{Glyphs.Cross} {message}[/]");
    }
}
