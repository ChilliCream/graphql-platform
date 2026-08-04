using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// Indicates that the given field, argument, input field, or enum value requires giving explicit
/// consent before being used.
/// </summary>
public sealed class RequiresOptIn
{
    /// <summary>
    /// Creates a new instance of <see cref="RequiresOptIn"/>.
    /// </summary>
    /// <param name="feature">
    /// The name of the feature that requires opt in.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="feature"/> is not a valid name.
    /// </exception>
    public RequiresOptIn(string feature)
    {
        if (!feature.IsValidGraphQLName())
        {
            throw new ArgumentException(
                TypeResources.RequiresOptInDirective_FeatureName_NotValid,
                nameof(feature));
        }

        Feature = feature;
    }

    /// <summary>
    /// The name of the feature that requires opt in.
    /// </summary>
    public string Feature { get; }
}
