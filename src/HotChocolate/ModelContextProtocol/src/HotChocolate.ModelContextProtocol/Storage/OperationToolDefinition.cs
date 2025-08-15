using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Directives;

namespace HotChocolate.ModelContextProtocol.Storage;

public record OperationToolDefinition(
    string Name,
    DocumentNode Document,
    string? Title = null,
    bool? DestructiveHint = null,
    bool? IdempotentHint = null,
    bool? OpenWorldHint = null);
