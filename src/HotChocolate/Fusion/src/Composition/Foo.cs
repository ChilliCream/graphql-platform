using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion.Composition;

public sealed class SubGraphConfiguration
{
    public SubGraphConfiguration(string name, string schema)
    {
        Name = name;
        Schema = schema;
    }

    public string Name { get; }

    public string Schema { get; }
}

public sealed class CompositionContext
{
    public CompositionContext(IReadOnlyList<SubGraphConfiguration> configurations)
    {
        Configurations = configurations;
    }

    public IReadOnlyList<SubGraphConfiguration> Configurations { get; }

    public List<Schema> SubGraphs { get; } = new();

    public List<EntityGroup> Entities { get; } = new();

    public Schema FusionGraph { get; } = new();

    public CancellationToken Abort { get; set; }

    public ICompositionLog Log { get; } = new CompositionLog();
}


public class CompositionLog : ICompositionLog
{
    public bool HasErrors { get; }

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

public sealed record LogEntry(
    string Message,
    string? Code = null,
    SchemaCoordinate? Coordinate = null,
    ITypeSystemMember? Member = null,
    Schema? Schema = null,
    Exception? Exception = null);
    
