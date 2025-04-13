using HotChocolate.Properties;
using HotChocolate.Utilities;

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
    /// <exception cref="ArgumentException">
    /// <paramref name="feature"/> is not a valid name.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="stability"/> is not a valid name.
    /// </exception>
    public OptInFeatureStabilityDirective(string feature, string stability)
    {
        if (!feature.IsValidGraphQLName())
        {
            throw new ArgumentException(
                TypeResources.OptInFeatureStabilityDirective_FeatureName_NotValid,
                nameof(feature));
        }

        if (!stability.IsValidGraphQLName())
        {
            throw new ArgumentException(
                TypeResources.OptInFeatureStabilityDirective_Stability_NotValid,
                nameof(stability));
        }

        Feature = feature;
        Stability = stability;
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
