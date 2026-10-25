namespace ChilliCream.Nitro.CommandLine;

internal sealed class ActivitySinkFactory : IActivitySinkFactory
{
    public IActivitySink Create(INitroConsole console, bool isInteractive)
    {
        if (isInteractive)
        {
            return new LiveActivitySink(console);
        }

        return new StreamingActivitySink(console);
    }
}
