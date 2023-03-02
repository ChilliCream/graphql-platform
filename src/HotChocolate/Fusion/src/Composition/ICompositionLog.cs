namespace HotChocolate.Fusion.Composition;

public interface ICompositionLog
{
    bool HasErrors { get; }

    void Write(LogEntry entry);
}
