namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsoleActivity : IAsyncDisposable
{
    void Update(string message);
}
