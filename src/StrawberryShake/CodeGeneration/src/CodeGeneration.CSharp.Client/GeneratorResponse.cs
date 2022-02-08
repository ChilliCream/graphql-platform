using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class GeneratorResponse : IMessage
{
    public GeneratorResponse(
        IReadOnlyList<GeneratorDocument>? documents = null,
        IReadOnlyList<GeneratorError>? errors = null)
    {
        if (documents is null && errors is null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        Documents = documents ?? Array.Empty<GeneratorDocument>();
        Errors = errors ?? Array.Empty<GeneratorError>();
    }

    public MessageKind Kind => MessageKind.Response;

    public IReadOnlyList<GeneratorDocument> Documents { get; }

    public IReadOnlyList<GeneratorError> Errors { get; }
}
