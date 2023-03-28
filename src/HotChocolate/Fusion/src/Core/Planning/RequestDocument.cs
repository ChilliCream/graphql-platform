using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal readonly record struct RequestDocument(DocumentNode Document, IReadOnlyList<string> Path);
