namespace ChilliCream.Nitro.CommandLine.Tests.Console;

internal sealed class SnapshotActivitySinkFactory : IActivitySinkFactory
{
    public IActivitySink Create(INitroConsole console, bool isInteractive)
    {
        if (isInteractive)
        {
            return new SnapshotActivitySink(console);
        }

        return new StreamingActivitySink(console);
    }
}
