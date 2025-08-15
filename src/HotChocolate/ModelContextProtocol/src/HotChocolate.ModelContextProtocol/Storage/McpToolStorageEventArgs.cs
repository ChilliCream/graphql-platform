using HotChocolate.Language;

namespace HotChocolate.ModelContextProtocol.Storage;

public record McpToolStorageEventArgs(
    string Name,
    McpToolStorageEventType Type,
    DocumentNode? Document = null);
