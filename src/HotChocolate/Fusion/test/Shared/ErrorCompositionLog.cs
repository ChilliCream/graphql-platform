using HotChocolate.Fusion.Composition;

namespace HotChocolate.Fusion.Shared;

public sealed class ErrorCompositionLog : ICompositionLog
{
    private readonly List<LogEntry> _errors = [];

    public bool HasErrors => _errors.Count > 0;

    public List<LogEntry> Errors => _errors;

    public void Write(LogEntry entry)
    {
        if(entry.Severity == LogSeverity.Error)
        {
            _errors.Add(entry);
        }
    }
}
