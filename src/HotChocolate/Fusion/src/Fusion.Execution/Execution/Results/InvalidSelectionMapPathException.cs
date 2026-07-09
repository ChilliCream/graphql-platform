using HotChocolate.Fusion.Language;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed class InvalidSelectionMapPathException(PathNode path)
    : Exception($"The path is invalid: {path}");
