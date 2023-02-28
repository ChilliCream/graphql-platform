using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

public interface ICompositionLog
{
    bool HasErrors { get; }

    void Info(LogEntry entry);

    void Info(
        string message,
        string? code = null,
        SchemaCoordinate? coordinate = null,
        ITypeSystemMember? member = null,
        Exception? exception = null);

    void Warning(LogEntry entry);

    void Warning(
        string message,
        string? code = null,
        SchemaCoordinate? coordinate = null,
        ITypeSystemMember? member = null,
        Exception? exception = null);

    void Error(LogEntry entry);

    void Error(
        string message,
        string? code = null,
        SchemaCoordinate? coordinate = null,
        ITypeSystemMember? member = null,
        Exception? exception = null);
}
