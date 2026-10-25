using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Matches when the received message carries a header for a given key, regardless of its value.
/// </summary>
/// <param name="key">The typed key of the header that must be present.</param>
/// <typeparam name="T">The type of the header value.</typeparam>
internal sealed class HeaderPresentCondition<T>(ContextDataKey<T> key) : RouteCondition
{
    /// <inheritdoc />
    public override bool Matches(IReceiveContext context)
        => context.Headers.TryGet(key, out _);

    /// <inheritdoc />
    public override RouteConditionDescription Describe()
        => new("HeaderPresent", key.Key, []);
}
