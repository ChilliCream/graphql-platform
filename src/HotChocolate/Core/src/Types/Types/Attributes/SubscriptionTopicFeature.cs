using HotChocolate.Types.Descriptors;

#nullable enable

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

    /// <summary>
    /// Gets the subscription topics of the schema that the descriptor context builds,
    /// and creates them when the context holds none yet.
    /// </summary>
    /// <param name="context">
    /// The descriptor context of the schema.
    /// </param>
    /// <returns>
    /// Returns the subscription topics of the schema.
    /// </returns>
    public static SubscriptionTopicFeature GetOrCreate(IDescriptorContext context)
    {
        var key = typeof(SubscriptionTopicFeature).FullName!;

        if (context.ContextData.TryGetValue(key, out var value)
            && value is SubscriptionTopicFeature feature)
        {
            return feature;
        }

        feature = new SubscriptionTopicFeature();
        context.ContextData[key] = feature;

        return feature;
    }
}
