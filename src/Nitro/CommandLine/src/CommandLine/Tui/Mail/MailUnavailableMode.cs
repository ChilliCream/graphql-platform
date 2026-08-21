using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Hosts the Mail tab when its actor failed to resolve: renders the
/// resolution failure instead of the mail board, so an invalid
/// <see cref="MailActor.EnvironmentVariableName"/> (or its
/// <see cref="MailActor.FallbackEnvironmentVariableName"/> fallback) does
/// not prevent the shell from opening on the Tasks tab.
/// </summary>
internal sealed class MailUnavailableMode : ITuiMode
{
    private readonly string _message;

    public MailUnavailableMode(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _message = message;
    }

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    public void OnEnter()
    {
    }

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message) => [];

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var errorStyle = ThemeTokens.GetStyle("toast.error.border").ToMarkup();
        var messageLine = new Markup($"[{errorStyle}]{Markup.Escape(_message)}[/]");
        var hintLine = new Markup(Markup.Escape(
            $"Set {MailActor.EnvironmentVariableName} to a valid agent name and reopen the shell."));

        return new Rows(messageLine, hintLine);
    }
}
