using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public sealed record LogEntry(
    string Message,
    string? Code = null,
    LogEntryKind Kind = LogEntryKind.Error,
    SchemaCoordinate? Coordinate = null,
    ITypeSystemMember? Member = null,
    Schema? Schema = null,
    Exception? Exception = null,
    object? Extension = null);

public enum LogEntryKind
{
    Info,
    Warning,
    Error
}
