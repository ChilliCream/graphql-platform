namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class GeneratorError
{
    public GeneratorError(
        string code,
        string title,
        string message,
        string? filePath = null,
        Location? location = null)
    {
        Code = code;
        Title = title;
        Message = message;
        FilePath = filePath;
        Location = location;
    }

    public string Code { get; }

    public string Title { get; }

    public string Message { get; }

    public string? FilePath { get; }

    public Location? Location { get; }
}
