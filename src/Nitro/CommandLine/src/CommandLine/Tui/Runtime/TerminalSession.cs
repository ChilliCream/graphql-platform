namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// Owns the terminal's alternate screen buffer and cursor visibility for the
/// lifetime of a TUI session.
/// </summary>
internal sealed class TerminalSession : IDisposable
{
    private readonly IAnsiConsole _console;
    private bool _disposed;

    public TerminalSession(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));

        _console.WriteAnsi(writer =>
        {
            writer.EnterAltScreen();
            writer.CursorHome();
        });
        _console.Cursor.Hide();
    }

    /// <summary>
    /// Restores the cursor and the primary screen buffer. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _console.Cursor.Show();
        _console.WriteAnsi(writer => writer.ExitAltScreen());
    }
}
