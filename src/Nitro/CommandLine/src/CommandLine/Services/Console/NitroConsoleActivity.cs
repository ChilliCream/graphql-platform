namespace ChilliCream.Nitro.CommandLine;

internal sealed class NitroConsoleActivity(INitroConsole console) : INitroConsoleActivity
{
    public void Update(string message)
    {
        // TODO: Indent this and render like a tree
        console.WriteLine(message);
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }

    public static INitroConsoleActivity Start(INitroConsole console, string title)
    {
        console.WriteLine(title);

        return new NitroConsoleActivity(console);
    }
}
