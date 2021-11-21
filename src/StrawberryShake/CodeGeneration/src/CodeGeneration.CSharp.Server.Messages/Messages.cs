using System;

namespace StrawberryShake.CodeGeneration.CSharp;

public class GeneratorResponse : ServerResponse
{
    public GeneratorResponse(
        SourceDocument[] documents,
        ServerError[] errors)
        : base(errors)
    {
        Documents = documents ??
            throw new ArgumentNullException(nameof(documents));
    }

    public SourceDocument[] Documents { get; }

    public static GeneratorResponse Success()
        => Success(Array.Empty<SourceDocument>());

    public static GeneratorResponse Success(SourceDocument[] documents)
    {
        return new GeneratorResponse(documents, Array.Empty<ServerError>());
    }

    public static GeneratorResponse Error(ServerError[] errors)
    {
        return new GeneratorResponse(Array.Empty<SourceDocument>(), errors);
    }

    public static GeneratorResponse Error(string message)
    {
        return new GeneratorResponse(
            Array.Empty<SourceDocument>(),
            new[] { new ServerError(message) });
    }
}

public class ServerError
{
    public ServerError(string message)
    {
        Message = message;
    }

    public string Message { get; }
}

public class ServerResponse
{
    public ServerResponse(ServerError[] errors)
    {
        Errors = errors ??
            throw new ArgumentNullException(nameof(errors));
    }

    public ServerError[] Errors { get; }

    public static ServerResponse Success { get; } = new(Array.Empty<ServerError>());

    public static ServerResponse Error(string message)
    {
        return new ServerResponse(new[] {new ServerError(message)});
    }
}
