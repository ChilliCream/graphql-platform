#nullable enable

namespace HotChocolate.Resolvers;

/// <summary>
/// This struct carries compiled resolvers for a field.
/// </summary>
public readonly ref struct FieldResolverDelegates
{
    /// <summary>
    /// Initializes a new instance of <see cref="FieldResolverDelegates"/>.
    /// </summary>
    public FieldResolverDelegates(
        FieldResolverDelegate? resolver = null,
        PureFieldDelegate? pureResolver = null)
    {
        if (pureResolver is not null && resolver is null)
        {
            resolver = context => new(pureResolver(context));
        }

        Resolver = resolver;
        PureResolver = pureResolver;
        HasResolvers = resolver is not null;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FieldResolverDelegates"/>.
    /// </summary>
    internal FieldResolverDelegates(FieldResolver resolver)
        : this(resolver.Resolver, resolver.PureResolver)
    {
    }

    /// <summary>
    /// Gets the async resolver which also is the default resolver.
    /// </summary>
    public FieldResolverDelegate? Resolver { get; }

    /// <summary>
    /// Gets a sync resolver which can be used in contexts where no services are needed
    /// and the method is considered pure.
    /// </summary>
    public PureFieldDelegate? PureResolver { get; }

    /// <summary>
    /// Defines if this instance has at least one resolver specified.
    /// </summary>
    public bool HasResolvers { get; }
}
