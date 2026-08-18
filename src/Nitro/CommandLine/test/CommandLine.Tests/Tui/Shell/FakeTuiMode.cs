using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

/// <summary>
/// A minimal <see cref="ITuiMode"/> test double that records every call it receives
/// and lets the test script the follow-up messages returned from <see cref="Handle"/>.
/// </summary>
internal sealed class FakeTuiMode : ITuiMode
{
    public KeyMap? KeyMap { get; set; }

    public bool EnterCalled { get; private set; }

    public List<(int Width, int Height)> ResizeCalls { get; } = [];

    public List<TuiMessage> HandledMessages { get; } = [];

    public List<(int Width, int Height)> RenderCalls { get; } = [];

    public Func<TuiMessage, IReadOnlyList<TuiMessage>> HandleResult { get; set; } = static _ => [];

    public string RenderText { get; set; } = "mode";

    public void OnEnter() => EnterCalled = true;

    public void OnResize(int width, int height) => ResizeCalls.Add((width, height));

    public IReadOnlyList<TuiMessage> Handle(TuiMessage message)
    {
        HandledMessages.Add(message);
        return HandleResult(message);
    }

    public IRenderable Render(int width, int height)
    {
        RenderCalls.Add((width, height));
        return new Markup(RenderText);
    }
}
