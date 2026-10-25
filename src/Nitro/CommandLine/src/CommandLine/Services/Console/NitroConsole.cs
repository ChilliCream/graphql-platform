using ChilliCream.Nitro.CommandLine.Results;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsole : INitroConsole
{
    private readonly IAnsiConsole _outConsole;
    private readonly IAnsiConsole _errorConsole;
    private readonly IActivitySinkFactory _activitySinkFactory;
    private OutputFormat? _outputFormat;
    private bool _hasWrittenOutput;

    public NitroConsole(
        IAnsiConsole outConsole,
        IAnsiConsole errorConsole,
        IActivitySinkFactory activitySinkFactory)
    {
        _outConsole = outConsole;
        _errorConsole = errorConsole;
        _activitySinkFactory = activitySinkFactory;
    }

    public bool IsInteractive =>
        IsHumanReadable && _outConsole.Profile.Capabilities.Interactive;

    public bool IsHumanReadable => _outputFormat is null;

    public bool HasWrittenOutput => _hasWrittenOutput;

    public IAnsiConsole Out => _outConsole;

    public IAnsiConsole Error => _errorConsole;

    public void WriteRawLine(string value)
    {
        _hasWrittenOutput = true;
        _outConsole.Profile.Out.Writer.WriteLine(value);
    }

    public void SetOutputFormat(OutputFormat format)
    {
        _outputFormat = format;
    }

    public INitroConsoleActivity StartActivity(string title, string failureMessage)
    {
        var sink = _activitySinkFactory.Create(this, IsInteractive);
        return NitroConsoleActivity.Start(sink, title, failureMessage);
    }

    public void Clear(bool home)
    {
        if (IsHumanReadable)
        {
            _outConsole.Clear(home);
        }
    }

    public void Write(IRenderable renderable)
    {
        if (IsHumanReadable)
        {
            _hasWrittenOutput = true;
            _outConsole.Write(renderable);
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
            _outConsole.WriteAnsi(action);
            return;
        }

        throw new ExitException(
            "Console runs in non interactive mode, yet a user interaction was attempted. "
            + "Check the documentation of the command to see all options");
    }

    public Profile Profile => _outConsole.Profile;

    public IAnsiConsoleCursor Cursor => _outConsole.Cursor;

    public IAnsiConsoleInput Input => _outConsole.Input;

    public IExclusivityMode ExclusivityMode =>
        IsInteractive
            ? _outConsole.ExclusivityMode
            : throw new ExitException(
                "Console runs in non interactive mode, yet a user interaction was attempted. "
                + "Check the documentation of the command to see all options");

    public RenderPipeline Pipeline => _outConsole.Pipeline;
}
