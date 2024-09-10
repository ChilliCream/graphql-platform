namespace StrawberryShake.Tools;

public class JsonConsoleOutputActivityData
{
    public JsonConsoleOutputActivityData(string text, string? path, TimeSpan time)
    {
        Text = text;
        Path = path;
        Time = time;
    }

    public string Text { get; }

    public  string? Path { get; }

    public TimeSpan Time { get; }
}
