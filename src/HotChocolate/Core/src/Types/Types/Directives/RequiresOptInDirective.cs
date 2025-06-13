#nullable enable

using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

/// <summary>
/// Indicates that the given field, argument, input field, or enum value requires giving explicit
/// consent before being used.
///
/// <code>
/// type Session {
///     id: ID!
///     title: String!
///     # [...]
///     startInstant: Instant @requiresOptIn(feature: "experimentalInstantApi")
///     endInstant: Instant @requiresOptIn(feature: "experimentalInstantApi")
/// }
/// </code>
/// </summary>
[DirectiveType(
    DirectiveNames.RequiresOptIn.Name,
    DirectiveLocation.ArgumentDefinition |
    DirectiveLocation.EnumValue |
    DirectiveLocation.FieldDefinition |
    DirectiveLocation.InputFieldDefinition,
    IsRepeatable = true)]
[GraphQLDescription(
    """
    Indicates that the given field, argument, input field, or enum value requires giving explicit
    consent before being used.

    type Session {
        id: ID!
        title: String!
        # [...]
        startInstant: Instant @requiresOptIn(feature: "experimentalInstantApi")
        endInstant: Instant @requiresOptIn(feature: "experimentalInstantApi")
    }
    """)]
public sealed class RequiresOptInDirective
{
    /// <summary>
    /// Creates a new instance of <see cref="RequiresOptInDirective"/>.
    /// </summary>
    /// <param name="feature">
    /// The name of the feature that requires opt in.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="feature"/> is not a valid name.
    /// </exception>
    public RequiresOptInDirective(string feature)
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
    [GraphQLDescription("The name of the feature that requires opt in.")]
    public string Feature { get; }
}
