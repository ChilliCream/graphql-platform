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