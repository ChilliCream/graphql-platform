namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsoleActivity : IAsyncDisposable
{
    void Update(string message);

    void Warning(string message);

    void Success(string? message = null);

    void Fail(string? message = null);
}
