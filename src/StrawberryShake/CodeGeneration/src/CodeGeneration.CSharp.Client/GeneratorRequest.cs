using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class GeneratorRequest : IMessage
{
    public GeneratorRequest(
        string configFileName,
        IReadOnlyList<string> documentFileNames,
        string? defaultNamespace = null,
        string? persistedQueryDirectory = null)
    {
        ConfigFileName = configFileName ??
            throw new ArgumentNullException(nameof(configFileName));
        DocumentFileNames = documentFileNames ??
            throw new ArgumentNullException(nameof(documentFileNames));
        DefaultNamespace = defaultNamespace;
        PersistedQueryDirectory = persistedQueryDirectory;
    }

    public MessageKind Kind => MessageKind.Request;

    public string ConfigFileName { get; }

    public IReadOnlyList<string> DocumentFileNames { get; }

    public string? DefaultNamespace { get; }

    public string? PersistedQueryDirectory { get; }
}
