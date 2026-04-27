using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsoleActivity : IAsyncDisposable
{
    void Update(string message, ActivityUpdateKind kind = ActivityUpdateKind.Regular, IRenderable? details = null);

    void Warning(string message);

    void Success(string message);

    void Fail(string message);

    void Fail();

    void Fail(IRenderable details, string? message = null);

    ValueTask FailAllAsync(IRenderable? details = null, string? message = null);

    INitroConsoleActivity StartChildActivity(string title, string failureMessage);
}
