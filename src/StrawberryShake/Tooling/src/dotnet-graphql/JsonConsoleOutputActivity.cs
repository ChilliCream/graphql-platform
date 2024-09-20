using System.Diagnostics;

namespace StrawberryShake.Tools;

public class JsonConsoleOutputActivity
    : IActivity
{
    private readonly JsonConsoleOutputData _data;
    private readonly string _activityText;
    private readonly string? _path;
    private readonly Action _errorReceived;
    private readonly Stopwatch _stopwatch;
    private bool _hasErrors;

    public JsonConsoleOutputActivity(
        JsonConsoleOutputData data,
        string activityText,
        string? path,
        Action errorReceived)
    {
        _data = data;
        _activityText = activityText;
        _path = path;
        _errorReceived = errorReceived;
        _stopwatch = Stopwatch.StartNew();
    }

    public void WriteError(HotChocolate.IError error)
    {
        _hasErrors = true;
        _data.Errors.Add(new JsonConsoleOutputErrorData(error));
        _errorReceived();
    }

    public void WriteErrors(IEnumerable<HotChocolate.IError> errors)
    {
        foreach (var error in errors)
        {
            _hasErrors = true;
            _data.Errors.Add(new JsonConsoleOutputErrorData(error));
            _errorReceived();
        }
    }

    public void Dispose()
    {
        _stopwatch.Stop();

        if (!_hasErrors)
        {
            _data.Activities.Add(new JsonConsoleOutputActivityData(
                _activityText,
                _path,
                _stopwatch.Elapsed));
        }
    }
}
