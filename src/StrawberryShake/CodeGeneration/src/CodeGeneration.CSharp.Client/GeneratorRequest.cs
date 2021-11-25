using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class GeneratorRequest
{
    public GeneratorRequest(
        string configFileName,
        IReadOnlyList<string> documentFileNames,
        string? ns = null,
        string? persistedQueryDirectory = null)
    {
        ConfigFileName = configFileName ??
            throw new ArgumentNullException(nameof(configFileName));
        DocumentFileNames = documentFileNames ??
            throw new ArgumentNullException(nameof(documentFileNames));
        DefaultNamespace = ns;
        PersistedQueryDirectory = persistedQueryDirectory;
    }

    public string ConfigFileName { get; }

    public IReadOnlyList<string> DocumentFileNames { get; }

    public string? DefaultNamespace { get; }

    public string? PersistedQueryDirectory { get; }
}
