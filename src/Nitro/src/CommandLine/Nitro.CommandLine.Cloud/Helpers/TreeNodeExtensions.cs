using ChilliCream.Nitro.CLI.Client;

namespace ChilliCream.Nitro.CLI;

internal static class TreeNodeExtensions
{
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
                "Unknown change. Try to update the version of ChilliCream.Nitro.CLI")
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
