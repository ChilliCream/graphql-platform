namespace ChilliCream.Nitro.CommandLine;

internal interface IActivitySinkFactory
{
    IActivitySink Create(INitroConsole console, bool isInteractive);
}
