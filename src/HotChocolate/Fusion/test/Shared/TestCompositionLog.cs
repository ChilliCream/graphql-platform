using HotChocolate.Fusion.Composition;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Shared;

public sealed class TestCompositionLog : ICompositionLog
{
    private readonly ITestOutputHelper _output;

    public TestCompositionLog(ITestOutputHelper output)
    {
        _output = output;
    }

    public bool HasErrors { get; private set; }

    public void Write(LogEntry entry)
    {
        if(entry.Severity == LogSeverity.Error)
        {
            HasErrors = true;
        }

        _output.WriteLine(entry.Message);
    }
}

public sealed class ErrorCompositionLog : ICompositionLog
{
    private readonly List<LogEntry> _errors = new();

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
