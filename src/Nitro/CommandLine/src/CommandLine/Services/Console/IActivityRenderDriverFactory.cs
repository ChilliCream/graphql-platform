namespace ChilliCream.Nitro.CommandLine;

internal interface IActivityRenderDriverFactory
{
    IActivityRenderDriver Create(INitroConsole console, ActivityTree tree);
}
