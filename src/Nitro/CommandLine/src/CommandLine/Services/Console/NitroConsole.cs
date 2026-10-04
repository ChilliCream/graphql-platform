using ChilliCream.Nitro.CommandLine.Results;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsole(
    IAnsiConsole outConsole,
    IAnsiConsole errorConsole,
    IActivitySinkFactory activitySinkFactory)
    : INitroConsole
{
    private OutputFormat? _outputFormat;
    private bool _hasWrittenOutput;

    public bool IsInteractive =>
        IsHumanReadable && outConsole.Profile.Capabilities.Interactive;

    public bool IsHumanReadable => _outputFormat is null;

    public bool HasWrittenOutput => _hasWrittenOutput;

    public IAnsiConsole Out => outConsole;

    public IAnsiConsole Error => errorConsole;

    public void WriteRawLine(string value)
    {
        outConsole.Profile.Out.Writer.WriteLine(value);
    }

    public void SetOutputFormat(OutputFormat format)
    {
        _outputFormat = format;
    }

    public INitroConsoleActivity StartActivity(string title, string failureMessage)
    {
        var sink = activitySinkFactory.Create(this, IsInteractive);
        return NitroConsoleActivity.Start(sink, title, failureMessage);
    }

    public void Clear(bool home)
    {
        if (IsHumanReadable)
        {
            outConsole.Clear(home);
        }
    }

    public void Write(IRenderable renderable)
    {
        if (IsHumanReadable)
        {
            _hasWrittenOutput = true;
            outConsole.Write(renderable);
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

    public void WriteAnsi(Action<AnsiWriter> action)
    {
        if (IsHumanReadable)
        {
            _hasWrittenOutput = true;
            outConsole.WriteAnsi(action);
            return;
        }

        throw new ExitException(
            "Console runs in non interactive mode, yet a user interaction was attempted. "
            + "Check the documentation of the command to see all options");
    }

    public Profile Profile => outConsole.Profile;

    public IAnsiConsoleCursor Cursor => outConsole.Cursor;

    public IAnsiConsoleInput Input => outConsole.Input;

    public IExclusivityMode ExclusivityMode =>
        IsInteractive
            ? outConsole.ExclusivityMode
            : throw new ExitException(
                "Console runs in non interactive mode, yet a user interaction was attempted. "
                + "Check the documentation of the command to see all options");

    public RenderPipeline Pipeline => outConsole.Pipeline;
}
