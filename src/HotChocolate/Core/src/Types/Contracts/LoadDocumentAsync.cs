using HotChocolate.Language;

namespace HotChocolate;

public delegate ValueTask<DocumentNode> LoadDocumentAsync(
    IServiceProvider services,
    CancellationToken cancellationToken);
