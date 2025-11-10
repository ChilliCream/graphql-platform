using HotChocolate.Language;

namespace HotChocolate.Exporters.OpenApi;

public sealed record OpenApiDocumentDefinition(string Id, DocumentNode Document);
