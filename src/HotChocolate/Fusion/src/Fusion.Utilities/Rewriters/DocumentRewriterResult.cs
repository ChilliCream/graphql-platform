using HotChocolate.Language;

namespace HotChocolate.Fusion.Rewriters;

/// <summary>
/// Represents the result of rewriting a GraphQL document through <see cref="DocumentRewriter"/>.
/// </summary>
/// <param name="Document">The rewritten document with conditionals normalized and selections folded.</param>
/// <param name="HasIncrementalParts">Indicates whether the document contains @defer or @stream directives for incremental delivery.</param>
public readonly record struct DocumentRewriterResult(DocumentNode Document, bool HasIncrementalParts);
