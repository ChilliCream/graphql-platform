namespace HotChocolate.Exporters.OpenApi;

public sealed record OpenApiDocumentStorageEventArgs(
    string Id,
    OpenApiDocumentStorageEventType Type,
    OpenApiDocumentDefinition? Definition = null);
