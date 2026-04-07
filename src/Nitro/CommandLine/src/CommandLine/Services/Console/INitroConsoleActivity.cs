using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsoleActivity : IAsyncDisposable
{
    void Update(string message, ActivityUpdateKind kind = ActivityUpdateKind.Regular);

    void Update(string message, IRenderable details, ActivityUpdateKind kind = ActivityUpdateKind.Regular);

    void Warning(string message);

    void Success(string message);

    void Fail(string message);

    void Fail();

    void Fail(IRenderable details);

    ValueTask FailAllAsync();

    INitroConsoleActivity StartChildActivity(string title, string failureMessage);
}
