using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// Queues toast messages and shows one at a time in a bottom-aligned status row,
/// advancing to the next queued toast once the current one expires.
/// </summary>
internal sealed class Toaster
{
    internal static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);

    private readonly Queue<(string Text, ToastStyle Style)> _pending = new();
    private (string Text, ToastStyle Style)? _current;
    private DateTimeOffset _currentShownAt;

    /// <summary>
    /// Queues a toast with <paramref name="text"/> and <paramref name="style"/>. If no
    /// toast is currently showing, it becomes the current toast immediately.
    /// </summary>
    public void Enqueue(string text, ToastStyle style, DateTimeOffset now)
    {
        _pending.Enqueue((text, style));

        if (_current is null)
        {
            Advance(now);
        }
    }

    /// <summary>
    /// Advances past the current toast once <see cref="Duration"/> has elapsed since it
    /// started showing. Returns whether the visible toast changed.
    /// </summary>
    public bool Tick(DateTimeOffset now)
    {
        if (_current is null || now - _currentShownAt < Duration)
        {
            return false;
        }

        Advance(now);
        return true;
    }

    /// <summary>
    /// Renders the current toast, or <see langword="null"/> when none is showing.
    /// </summary>
    public IRenderable? Render()
    {
        if (_current is not { } toast)
        {
            return null;
        }

        var (icon, token) = toast.Style switch
        {
            ToastStyle.Success => ("✔", "toast.success.border"),
            ToastStyle.Warn => ("!", "toast.warn.border"),
            ToastStyle.Error => ("✘", "toast.error.border"),
            _ => ("i", "toast.info.border")
        };

        var style = ThemeTokens.GetStyle(token).ToMarkup();
        var text = Markup.Escape(toast.Text);

        return new Markup($"[{style}]{icon} {text}[/]");
    }

    private void Advance(DateTimeOffset now)
    {
        _current = _pending.Count > 0 ? _pending.Dequeue() : null;
        _currentShownAt = now;
    }
}
