namespace HotChocolate.Fusion.Errors;

public sealed class CompositionError(string message)
{
    public string Message { get; } = message;
}
