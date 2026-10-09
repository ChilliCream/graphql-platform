using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Introspection;

/// <summary>
/// Provides helpers for determining whether a type-system member is visible
/// to an introspection caller based on the caller's opted-in features.
/// </summary>
internal static class OptInIntrospectionHelper
{
    /// <summary>
    /// Returns <see langword="true"/> when the member represented by
    /// <paramref name="directives"/> should be included in an introspection response
    /// for a caller that has opted into the features listed in
    /// <paramref name="includeOptIn"/>.
    /// </summary>
    /// <param name="directives">
    /// The directive collection of the type-system member being tested.
    /// </param>
    /// <param name="includeOptIn">
    /// The set of opt-in feature names the caller has enabled.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the member carries no <c>@requiresOptIn</c> directives,
    /// or if at least one of its required features is present in
    /// <paramref name="includeOptIn"/>; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsIncluded(
        IReadOnlyDirectiveCollection directives,
        string[] includeOptIn)
    {
        var requiresOptIn = false;

        foreach (var directive in directives)
        {
            if (!directive.Name.Equals(
                    DirectiveNames.RequiresOptIn.Name,
                    StringComparison.Ordinal))
            {
                continue;
            }

            requiresOptIn = true;

            if (directive is FusionDirective fusionDirective
                && fusionDirective.Arguments.TryGetValue(
                    DirectiveNames.RequiresOptIn.Arguments.Feature,
                    out var argValue)
                && argValue is StringValueNode feature
                && includeOptIn.Contains(feature.Value))
            {
                return true;
            }
        }

        return !requiresOptIn;
    }
}
