namespace HotChocolate.Fusion.Results;

public sealed class Error(string message)
{
    public string Message { get; } = message;
}
