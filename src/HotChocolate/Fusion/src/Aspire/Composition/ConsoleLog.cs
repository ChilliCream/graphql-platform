namespace HotChocolate.Fusion.Composition;

internal sealed class ConsoleLog : ICompositionLog
{
    public bool HasErrors { get; private set; }

    public void Write(LogEntry e)
    {
        if (e.Severity is LogSeverity.Error)
        {
            HasErrors = true;
        }

        if (e.Code is null)
        {
            Console.WriteLine($"{e.Severity}: {e.Message}");
        }
        else if (e.Coordinate is null)
        {
            Console.WriteLine($"{e.Severity}: {e.Code} {e.Message}");
        }
        else
        {
            Console.WriteLine($"{e.Severity}: {e.Code} {e.Message} {e.Coordinate}");
        }
    }
}
