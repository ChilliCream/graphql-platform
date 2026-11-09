namespace HotChocolate.Types;

/// <summary>
/// Collects the fixed topics of the subscription fields of a schema.
/// </summary>
internal sealed class SubscriptionTopicFeature
{
    /// <summary>
    /// Gets the topics that subscription fields subscribe to by a name that does not depend
    /// on their arguments. A topic resolved from argument values must not be one of them.
    /// </summary>
    public HashSet<string> FixedTopics { get; } = [];
}
