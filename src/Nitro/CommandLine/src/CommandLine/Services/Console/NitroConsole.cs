using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsole(
    IAnsiConsole console,
    IAnsiConsole errorConsole,
    IEnvironmentVariableProvider environmentVariables)
    : INitroConsole
{
    private OutputFormat? _outputFormat;
    private bool _hasWrittenOutput;

    public bool IsInteractive =>
        console.Profile.Capabilities.Interactive
        && !IsNonInteractiveEnvironment();

    public bool IsHumanReadable => _outputFormat is null;

    public bool HasWrittenOutput => _hasWrittenOutput;

    public IAnsiConsole Out => console;

    public IAnsiConsole Error => errorConsole;

    public void SetOutputFormat(OutputFormat format)
    {
       _outputFormat = format;
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
            _hasWrittenOutput = true;
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

    private bool IsNonInteractiveEnvironment()
    {
        var value = environmentVariables.GetEnvironmentVariable("NITRO_NON_INTERACTIVE");
        return value is "1" or "true";
    }
}
