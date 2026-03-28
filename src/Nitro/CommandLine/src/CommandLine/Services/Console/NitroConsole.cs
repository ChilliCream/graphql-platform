using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsole(
    IAnsiConsole console,
    TextWriter? errorWriter = null)
    : INitroConsole
{
    public bool IsInteractive => console.Profile.Capabilities.Interactive;

    public void WriteErrorLine(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (errorWriter is not null)
        {
            errorWriter.Write(message + Environment.NewLine);
            return;
        }

        console.MarkupLine(message);
    }

    public void Clear(bool home)
    {
        if (IsInteractive)
        {
            console.Clear(home);
        }
    }

    public void Write(IRenderable renderable)
    {
        if (IsInteractive)
        {
            console.Write(renderable);
            return;
        }

        if (renderable is Text or Paragraph or Markup)
        {
            return;
        }

        throw new ExitException(
            "Console runs in non interactive mode, yet a user interaction was attempted. "
            + "Check the documentation of the command to see all options");
    }

    public Profile Profile => console.Profile;
    public IAnsiConsoleCursor Cursor => console.Cursor;
    public IAnsiConsoleInput Input => console.Input;

    public IExclusivityMode ExclusivityMode =>
        IsInteractive
            ? console.ExclusivityMode
            : throw new ExitException(
                "Console runs in non interactive mode, yet a user interaction was attempted. "
                + "Check the documentation of the command to see all options");

    public RenderPipeline Pipeline => console.Pipeline;
}
