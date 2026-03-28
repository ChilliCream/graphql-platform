namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleActivity(INitroConsole console) : INitroConsoleActivity
{
    private bool _completed;

    public void Update(string message)
    {
        console.WriteLine("├── " + message);
    }

    public void Success(string? message = null)
    {
        Complete(message ?? "Done!");
    }

    public void Fail(string? message = null)
    {
        Complete(message ?? "Failed!");
    }

    public ValueTask DisposeAsync()
    {
        Fail();

        return default;
    }

    private void Complete(string message)
    {
        if (_completed)
        {
            return;
        }

        console.WriteLine("└── " + message);

        _completed = true;
    }

    public static INitroConsoleActivity Start(INitroConsole console, string title)
    {
        console.WriteLine(title);

        return new NitroConsoleActivity(console);
    }
}
