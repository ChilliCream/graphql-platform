#nullable enable

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
    WellKnownDirectives.RequiresOptIn,
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
    /// <exception cref="ArgumentNullException">
    /// <paramref name="feature"/> is <c>null</c>.
    /// </exception>
    public RequiresOptInDirective(string feature)
    {
        Feature = feature ?? throw new ArgumentNullException(nameof(feature));
    }

    /// <summary>
    /// The name of the feature that requires opt in.
    /// </summary>
    [GraphQLDescription("The name of the feature that requires opt in.")]
    public string Feature { get; }
}
