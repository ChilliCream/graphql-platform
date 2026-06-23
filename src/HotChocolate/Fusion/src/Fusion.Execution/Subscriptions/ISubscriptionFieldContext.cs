using HotChocolate.Language;

namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Provides subscription field data that a broker can use while opening an event stream.
/// </summary>
public interface ISubscriptionFieldContext
{
    /// <summary>
    /// Gets the subscription root field argument values with variables substituted, keyed by
    /// argument name.
    /// </summary>
    IReadOnlyDictionary<string, IValueNode> Arguments { get; }
}
