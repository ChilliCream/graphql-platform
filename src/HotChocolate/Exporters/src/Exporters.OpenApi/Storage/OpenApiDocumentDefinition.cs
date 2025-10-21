using HotChocolate.Language;

namespace HotChocolate.Exporters.OpenApi;

// TODO: Different name
public sealed record OpenApiDocumentDefinition(string Id, DocumentNode Document);
