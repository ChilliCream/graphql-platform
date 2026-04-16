using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsoleActivity : IAsyncDisposable
{
    void Update(string message, ActivityUpdateKind kind = ActivityUpdateKind.Regular, IRenderable? details = null);

    void Warning(string message);

    void Success(string message);

    void Fail(string message);

    void Fail();

    ValueTask FailAllAsync(IRenderable? details = null);

    INitroConsoleActivity StartChildActivity(string title, string failureMessage);
}
