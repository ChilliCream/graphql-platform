namespace ChilliCream.Nitro.CommandLine.Tests.Console;

internal sealed class SnapshotActivityRenderDriver(INitroConsole console, ActivityTree tree)
    : IActivityRenderDriver
{
    public Task Completion => Task.CompletedTask;

    public void Stop()
    {
        console.Write(tree);
    }
}
