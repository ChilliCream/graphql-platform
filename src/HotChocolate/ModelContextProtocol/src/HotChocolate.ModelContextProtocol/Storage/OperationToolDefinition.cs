using HotChocolate.Language;

namespace HotChocolate.ModelContextProtocol.Storage;

public record OperationToolDefinition(
    string Name,
    DocumentNode Document,
    string? Title = null,
    bool? DestructiveHint = null,
    bool? IdempotentHint = null,
    bool? OpenWorldHint = null);
