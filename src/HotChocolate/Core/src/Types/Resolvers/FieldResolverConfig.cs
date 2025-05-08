#nullable enable

namespace HotChocolate.Resolvers;

internal readonly struct FieldResolverConfiguration
{
    private readonly bool _isEmpty;

    public FieldResolverConfiguration(
        SchemaCoordinate fieldCoordinate,
        FieldResolverDelegate? resolver = null,
        PureFieldDelegate? pureResolver = null,
        Type? resultType = null)
    {
        if (resolver is null && pureResolver is null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        FieldCoordinate = fieldCoordinate;
        PureResolver = pureResolver;
        Resolver = resolver ?? (ctx => new ValueTask<object?>(pureResolver!(ctx)));
        ResultType = resultType ?? typeof(object);
        _isEmpty = false;
    }

    public SchemaCoordinate FieldCoordinate { get; }

    public FieldResolverDelegate Resolver { get; }

    public PureFieldDelegate? PureResolver { get; }

    public Type ResultType { get; }

    public bool IsDefault => !_isEmpty;

    public FieldResolverDelegates ToFieldResolverDelegates()
        => new(Resolver, PureResolver);
}
