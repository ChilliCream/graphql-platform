namespace HotChocolate.Types;

public sealed class OptInFeatureStabilityDirective
{
    /// <summary>
    /// Creates a new instance of <see cref="OptInFeatureStabilityDirective"/>.
    /// </summary>
    /// <param name="feature">
    /// The name of the feature for which to set the stability.
    /// </param>
    /// <param name="stability">
    /// The stability level of the feature.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="feature"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="stability"/> is <c>null</c>.
    /// </exception>
    public OptInFeatureStabilityDirective(string feature, string stability)
    {
        Feature = feature ?? throw new ArgumentNullException(nameof(feature));
        Stability = stability ?? throw new ArgumentNullException(nameof(stability));
    }

    /// <summary>
    /// The name of the feature for which to set the stability.
    /// </summary>
    [GraphQLDescription("The name of the feature for which to set the stability.")]
    public string Feature { get; }

    /// <summary>
    /// The stability level of the feature.
    /// </summary>
    [GraphQLDescription("The stability level of the feature.")]
    public string Stability { get; }
}
