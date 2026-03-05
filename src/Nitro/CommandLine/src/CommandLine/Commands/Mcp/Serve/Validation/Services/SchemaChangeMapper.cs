using ChilliCream.Nitro.CommandLine.Client;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Validation.Services;

internal static class SchemaChangeMapper
{
    public static SchemaChangeEntry Map(ISchemaChangeLogEntry entry)
    {
        return entry switch
        {
            ITypeSystemMemberAddedChange c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "TypeSystemMemberAddedChange",
                c.Coordinate,
                $"Schema member '{c.Coordinate}' was added."),

            ITypeSystemMemberRemovedChange c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "TypeSystemMemberRemovedChange",
                c.Coordinate,
                $"Schema member '{c.Coordinate}' was removed."),

            IFieldRemovedChange c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "FieldRemovedChange",
                c.Coordinate,
                $"Field '{c.FieldName}' was removed from type '{c.TypeName}'."),

            IFieldAddedChange c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "FieldAddedChange",
                c.Coordinate,
                $"Field '{c.FieldName}' was added to type '{c.TypeName}'."),

            IOutputFieldChanged c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "OutputFieldChanged",
                c.Coordinate,
                $"Field '{c.FieldName}' at '{c.Coordinate}' was modified."),

            IInputFieldChanged c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "InputFieldChanged",
                c.Coordinate,
                $"Input field '{c.FieldName}' at '{c.Coordinate}' was modified."),

            IEnumValueRemoved c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "EnumValueRemoved",
                c.Coordinate,
                $"Enum value '{c.Coordinate}' was removed."),

            IEnumValueAdded c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "EnumValueAdded",
                c.Coordinate,
                $"Enum value '{c.Coordinate}' was added."),

            IUnionMemberRemoved c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "UnionMemberRemoved",
                null,
                $"Union member '{c.TypeName}' was removed."),

            IUnionMemberAdded c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "UnionMemberAdded",
                null,
                $"Union member '{c.TypeName}' was added."),

            IObjectModifiedChange c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "ObjectModifiedChange",
                c.Coordinate,
                $"Object type '{c.Coordinate}' was modified."),

            IDirectiveModifiedChange c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "DirectiveModifiedChange",
                c.Coordinate,
                $"Directive '{c.Coordinate}' was modified."),

            IEnumModifiedChange c => new SchemaChangeEntry(
                FormatSeverity(c.Severity),
                "EnumModifiedChange",
                c.Coordinate,
                $"Enum '{c.Coordinate}' was modified."),

            _ when entry is ISchemaChange sc => new SchemaChangeEntry(
                FormatSeverity(sc.Severity),
                entry.GetType().Name,
                null,
                $"Change of type '{entry.GetType().Name}'."),

            _ => new SchemaChangeEntry("SAFE", entry.GetType().Name, null, $"Change of type '{entry.GetType().Name}'.")
        };
    }

    public static IReadOnlyList<SchemaChangeEntry> MapAll(IEnumerable<ISchemaChangeLogEntry> entries)
    {
        return entries
            .Select(Map)
            .OrderBy(static c =>
                c.Severity switch
                {
                    "BREAKING" => 0,
                    "DANGEROUS" => 1,
                    "SAFE" => 2,
                    _ => 3
                }
            )
            .ToList();
    }

    private static string FormatSeverity(SchemaChangeSeverity severity)
    {
        return severity switch
        {
            SchemaChangeSeverity.Breaking => "BREAKING",
            SchemaChangeSeverity.Dangerous => "DANGEROUS",
            SchemaChangeSeverity.Safe => "SAFE",
            _ => severity.ToString().ToUpperInvariant()
        };
    }
}
