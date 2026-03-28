using ChilliCream.Nitro.CommandLine.Results;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsole(
    IAnsiConsole console,
    TextWriter outWriter,
    TextWriter errorWriter)
    : INitroConsole
{
    private OutputFormat? _outputFormat;

    public bool IsInteractive => console.Profile.Capabilities.Interactive;

    public bool IsHumanReadable => _outputFormat is null;

    public TextWriter Out => outWriter;

    public TextWriter Error => errorWriter;

    public void SetOutputFormat(OutputFormat format)
    {
       _outputFormat = format;
    }

    public void WriteDirectly(string message)
    {
        console.WriteLine(message);
    }

    public void Clear(bool home)
    {
        if (IsHumanReadable)
        {
            console.Clear(home);
        }
    }

    public void Write(IRenderable renderable)
    {
        if (IsHumanReadable)
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

    public IExclusivityMode ExclusivityMode => console.ExclusivityMode;
        // IsInteractive
        //     ? console.ExclusivityMode
        //     : throw new ExitException(
        //         "Console runs in non interactive mode, yet a user interaction was attempted. "
        //         + "Check the documentation of the command to see all options");

    public RenderPipeline Pipeline => console.Pipeline;
}
