namespace ChilliCream.Nitro.CommandLine.Tests.Console;

internal sealed class SnapshotActivityRenderDriverFactory : IActivityRenderDriverFactory
{
    public IActivityRenderDriver Create(INitroConsole console, ActivityTree tree)
    {
        return new SnapshotActivityRenderDriver(console, tree);
    }
}
