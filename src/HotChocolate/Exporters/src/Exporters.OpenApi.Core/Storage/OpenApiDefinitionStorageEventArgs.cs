namespace HotChocolate.Exporters.OpenApi;

public sealed record OpenApiDefinitionStorageEventArgs(
    string Id,
    OpenApiDefinitionStorageEventType Type,
    OpenApiDocumentDefinition? Definition = null);
