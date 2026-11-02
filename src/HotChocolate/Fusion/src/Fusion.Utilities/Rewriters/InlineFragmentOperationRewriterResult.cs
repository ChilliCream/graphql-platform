using HotChocolate.Language;

namespace HotChocolate.Fusion.Rewriters;

/// <summary>
/// Represents the result of flattening a GraphQL document by inlining fragment spreads and merging inline fragments.
/// </summary>
/// <param name="Document">The flattened document with all fragments inlined into the operation.</param>
/// <param name="HasIncrementalParts">Indicates whether the document contains @defer or @stream directives for incremental delivery.</param>
public readonly record struct InlineFragmentOperationRewriterResult(DocumentNode Document, bool HasIncrementalParts);
