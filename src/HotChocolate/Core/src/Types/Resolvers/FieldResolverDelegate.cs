using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// This delegates describes the resolver interface that the execution engine uses to
    /// resolve the data of a field.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <returns>
    /// Returns the resolver result.
    /// </returns>
    public delegate ValueTask<object?> FieldResolverDelegate(IResolverContext context);

    /// <summary>
    /// This delegates represents a pure resolver that is side-effect free and sync.
    /// </summary>
    /// <param name="context">The resolver context.</param>
    /// <returns>
    /// Returns the resolver result.
    /// </returns>
    public delegate object? PureFieldResolverDelegate(IResolverContext context);
}
