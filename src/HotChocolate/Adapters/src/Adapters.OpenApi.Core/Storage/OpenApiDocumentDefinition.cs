using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiDocumentDefinition(string Id, DocumentNode Document);
