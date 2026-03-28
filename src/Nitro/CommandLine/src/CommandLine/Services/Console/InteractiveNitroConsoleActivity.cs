namespace ChilliCream.Nitro.CommandLine;

internal interface INitroConsoleActivity : IAsyncDisposable
{
    void Update(string message);
}

internal sealed class InteractiveNitroConsoleActivity : INitroConsoleActivity
{
    private readonly TaskCompletionSource _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _spinnerTask;
    private StatusContext? _context;

    public void Update(string message)
    {
        _context?.Status(message);
    }

    public async ValueTask DisposeAsync()
    {
        _completion.TrySetResult();

        if (_spinnerTask is not null)
        {
            await _spinnerTask;
        }
    }

    private Task WaitForCompletionAsync() => _completion.Task;

    private void SetContext(StatusContext context) => _context = context;

    private void SetSpinnerTask(Task spinnerTask) => _spinnerTask = spinnerTask;

    public static INitroConsoleActivity Start(string title, IAnsiConsole console)
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
