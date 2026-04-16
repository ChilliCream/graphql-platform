namespace ChilliCream.Nitro.CommandLine;

internal sealed class LiveActivityRenderDriverFactory : IActivityRenderDriverFactory
{
    public IActivityRenderDriver Create(INitroConsole console, ActivityTree tree)
    {
        return new LiveActivityRenderDriver(console, tree);
    }
}
