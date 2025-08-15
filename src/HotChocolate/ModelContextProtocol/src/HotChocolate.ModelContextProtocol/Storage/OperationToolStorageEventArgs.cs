using HotChocolate.Language;

namespace HotChocolate.ModelContextProtocol.Storage;

public record OperationToolStorageEventArgs(
    string Name,
    OperationToolStorageEventType Type,
    DocumentNode? Document = null);
