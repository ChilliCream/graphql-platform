namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsoleActivity : IAsyncDisposable
{
    void Update(string message);

    void Warning(string message);

    void Success(string message);

    void Fail(string message);

    void Fail();
}
