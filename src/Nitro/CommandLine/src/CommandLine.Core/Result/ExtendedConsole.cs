using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

public sealed class ExtendedConsole(IAnsiConsole console) : IExtendedConsole
{
    public bool IsInteractive { get; set; } = true;

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
        }
        else if (renderable is Text or Paragraph or Markup)
        {
            // omit the text
        }
        else
        {
            throw ThrowHelper.ConsoleInNonInteractiveMode();
        }
    }

    public Profile Profile => console.Profile;
    public IAnsiConsoleCursor Cursor => console.Cursor;
    public IAnsiConsoleInput Input => console.Input;

    public IExclusivityMode ExclusivityMode
    {
        get
        {
            if (IsInteractive)
            {
                return console.ExclusivityMode;
            }

            throw ThrowHelper.ConsoleInNonInteractiveMode();
        }
    }

    public RenderPipeline Pipeline => console.Pipeline;

    public static ExtendedConsole Create(IAnsiConsole console)
    {
        return new ExtendedConsole(console);
    }
}

file static class ThrowHelper
{
    public static Exception ConsoleInNonInteractiveMode()
    {
        return new ExitException(
            "Console runs in non interactive mode, yet a user interaction was attempted. "
            + "Check the documentation of the command to see all options");
    }
}
