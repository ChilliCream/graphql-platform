using System.Diagnostics;

namespace StrawberryShake.Tools;

public class DefaultConsoleOutputActivity
    : IActivity
{
    private readonly string _activityText;
    private readonly string? _path;
    private readonly Action _errorReceived;
    private readonly Stopwatch _stopwatch;
    private bool _hasErrors;

    public DefaultConsoleOutputActivity(string activityText, string? path, Action errorReceived)
    {
        _activityText = activityText;
        _path = path;
        _errorReceived = errorReceived;
        _stopwatch = Stopwatch.StartNew();
        Console.WriteLine($"{activityText} started.");
    }

    public void WriteError(HotChocolate.IError error)
    {
        _hasErrors = true;
        error.Write();
        _errorReceived();
    }

    public void WriteErrors(IEnumerable<HotChocolate.IError> errors)
    {
        foreach (var error in errors)
        {
            _hasErrors = true;
            error.Write();
            _errorReceived();
        }
    }

    public void Dispose()
    {
        _stopwatch.Stop();

        if (!_hasErrors)
        {
            Console.Write(
                $"{_activityText} completed in " +
                $"{_stopwatch.ElapsedMilliseconds} ms");
            if (_path is { })
            {
                Console.WriteLine($"for {_path}.");
            }
            else
            {
                Console.WriteLine();
            }
        }
    }
}
