using HotChocolate.Fusion.Composition;
using Xunit.Abstractions;

namespace HotChocolate.Fusion.Shared;

public sealed class TestCompositionLog(ITestOutputHelper output) : ICompositionLog
{
    public bool HasErrors { get; private set; }

    public void Write(LogEntry entry)
    {
        if(entry.Severity == LogSeverity.Error)
        {
            HasErrors = true;
        }

        output.WriteLine(entry.Message);
    }
}
