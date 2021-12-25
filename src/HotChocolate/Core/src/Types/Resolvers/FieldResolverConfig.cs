using System;

#nullable enable

namespace HotChocolate.Resolvers;

internal readonly struct FieldResolverConfig
{
    private readonly bool _isEmpty;

    public FieldResolverConfig(
        FieldCoordinate field,
        FieldResolverDelegate? resolver = null,
        PureFieldDelegate? pureResolver = null,
        Type? resultType = null)
    {
        if (resolver is null && pureResolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        Field = field;
        PureResolver = pureResolver;
        Resolver = resolver ?? new FieldResolverDelegate(ctx => new(pureResolver!(ctx)));
        ResultType = resultType ?? typeof(object);
        _isEmpty = false;
    }

    public FieldCoordinate Field { get; }

    public FieldResolverDelegate Resolver { get; }

    public PureFieldDelegate? PureResolver { get; }

    public Type ResultType { get; }

    public bool IsDefault => !_isEmpty;

    public FieldResolverDelegates ToFieldResolverDelegates()
        => new(Resolver, PureResolver);
}
