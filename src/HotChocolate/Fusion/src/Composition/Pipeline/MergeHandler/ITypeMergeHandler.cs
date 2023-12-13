using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Defines a type handler that is responsible for merging a group of types
/// into a single distributed type on the fusion graph.
/// </summary>
internal interface ITypeMergeHandler
{
    /// <summary>
    /// Gets the type kind that can be merged by this handler.
    /// </summary>
    TypeKind Kind { get; }
    
    /// <summary>
    /// Merges a group of types into a single distributed type on the fusion graph
    /// </summary>
    /// <param name="context">The composition context.</param>
    /// <param name="typeGroup">The group of types to merge.</param>
    /// <returns>
    /// A <see cref="MergeStatus"/> that indicates if the type group was merged.
    /// </returns>
    MergeStatus Merge(CompositionContext context, TypeGroup typeGroup);
}
