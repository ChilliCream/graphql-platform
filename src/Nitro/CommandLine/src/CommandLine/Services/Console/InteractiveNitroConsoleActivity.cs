using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine;

internal sealed class InteractiveNitroConsoleActivity : INitroConsoleActivity
{
    private readonly TaskCompletionSource _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _spinnerTask;
    private StatusContext? _context;
    private bool _completed;

    public void Update(string message)
    {
        _context?.Status(message);
    }

    public void Warning(string message)
    {
        _context?.Status(Glyphs.ExclamationMark.Space() + message);
    }

    public void Success(string? message = null)
    {
        Complete(message);
    }

    public void Fail(string? message = null)
    {
        Complete(message);
    }

    public async ValueTask DisposeAsync()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;

        _completion.TrySetResult();

        if (_spinnerTask is { } spinnerTask)
        {
            _spinnerTask = null;
            await spinnerTask;
        }

        _context = null;
    }

    // TODO: This should be async
    private void Complete(string? message)
    {
        if (_completed)
        {
            return;
        }

        if (!string.IsNullOrEmpty(message))
        {
            _context?.Status(message);
        }

        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private Task WaitForCompletionAsync() => _completion.Task;

    private void SetContext(StatusContext context) => _context = context;

    private void SetSpinnerTask(Task spinnerTask) => _spinnerTask = spinnerTask;

    public static INitroConsoleActivity Start(INitroConsole console, string title)
    {
        var activity = new InteractiveNitroConsoleActivity();

        var spinnerTask = console
            .Status()
            .Spinner(Spinner.Known.BouncingBar)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync(
                title,
                async context =>
                {
                    activity.SetContext(context);

                    await activity.WaitForCompletionAsync();
                });

        activity.SetSpinnerTask(spinnerTask);

        return activity;
    }
}
