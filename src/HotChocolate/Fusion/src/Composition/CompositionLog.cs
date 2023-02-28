using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public class CompositionLog : ICompositionLog
{
    public bool HasErrors { get; } = false;

    public void Info(LogEntry entry) { }

    public void Info(
        string message,
        string? code = null,
        SchemaCoordinate? coordinate = null,
        ITypeSystemMember? member = null,
        Exception? exception = null) { }

    public void Warning(LogEntry entry) { }

    public void Warning(
        string message,
        string? code = null,
        SchemaCoordinate? coordinate = null,
        ITypeSystemMember? member = null,
        Exception? exception = null) { }

    public void Error(LogEntry entry) { }

    public void Error(
        string message,
        string? code = null,
        SchemaCoordinate? coordinate = null,
        ITypeSystemMember? member = null,
        Exception? exception = null) { }
}
