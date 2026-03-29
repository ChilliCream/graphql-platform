using ChilliCream.Nitro.CommandLine.Helpers;

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
        message ??= "Done!";

        Complete(Glyphs.Check.Space() + message);
    }

    public void Fail(string? message = null)
    {
        message ??= "Failed!";

        Complete(Glyphs.Cross.Space() + message);
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

        console.MarkupLine("└── " + message);

        _completed = true;
    }

    public static INitroConsoleActivity Start(INitroConsole console, string title)
    {
        console.WriteLine(title);

        return new NitroConsoleActivity(console);
    }
}
