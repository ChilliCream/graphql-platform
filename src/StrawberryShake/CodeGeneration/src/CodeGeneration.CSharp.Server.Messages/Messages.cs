namespace StrawberryShake.CodeGeneration.CSharp;

public class GeneratorResponse
{
    public SourceDocument[] Documents { get; set; }

    public GeneratorError[] Errors { get; set; }
}

public class GeneratorError
{
    public string Message { get; set; }
}

public class ServerResponse
{
    public GeneratorError[] Errors { get; set; }
}