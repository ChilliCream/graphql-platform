namespace StrawberryShake.Tools;

public class JsonConsoleOutputData
{
    public List<JsonConsoleOutputActivityData> Activities { get; } =
        [];

    public List<JsonConsoleOutputErrorData> Errors { get; } =
        [];

    public  List<string> CreatedFiles { get; } =
        [];
}
