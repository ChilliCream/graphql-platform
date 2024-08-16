using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections.Context;

/// <summary>
/// Common extensions of <see cref="IResolverContext"/> for <see cref="ISelectedField"/>
/// </summary>
public static class ResolverContextSelectionExtensions
{
    /// <summary>
    /// Gets the <see cref="ISelectedField"/> of the current resolver.
    /// </summary>
    /// <param name="context">The resolver context</param>
    /// Returns the <see cref="ISelectedField"/> of the current resolver.
    public static ISelectedField GetSelectedField(this IResolverContext context)
        => new SelectedField(context, context.Selection);
}
