using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public sealed record LogEntry(
    string Message,
    string? Code = null,
    SchemaCoordinate? Coordinate = null,
    ITypeSystemMember? Member = null,
    Schema? Schema = null,
    Exception? Exception = null,
    object? Extension = null);
