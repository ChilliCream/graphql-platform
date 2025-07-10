using HotChocolate.Language;
using HotChocolate.Types;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Extensions;

internal static class DirectivesProviderExtensions
{
    public static string? GetIsFieldSelectionMap(this IDirectivesProvider type)
    {
        var isDirective = type.Directives.FirstOrDefault(d => d.Name == WellKnownDirectiveNames.Is);

        if (isDirective?.Arguments[ArgumentNames.Field] is StringValueNode fieldArgument)
        {
            return fieldArgument.Value;
        }

        return null;
    }

    public static string? GetProvidesSelectionSet(this IDirectivesProvider type)
    {
        var providesDirective =
            type.Directives.FirstOrDefault(d => d.Name == WellKnownDirectiveNames.Provides);

        if (providesDirective?.Arguments[ArgumentNames.Fields] is StringValueNode fieldsArgument)
        {
            return fieldsArgument.Value;
        }

        return null;
    }

    public static bool HasExternalDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.External);
    }

    public static bool HasFusionInaccessibleDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.FusionInaccessible);
    }

    public static bool HasFusionRequiresDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.FusionRequires);
    }

    public static bool HasInaccessibleDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.Inaccessible);
    }

    public static bool HasIsDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.Is);
    }

    public static bool HasLookupDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.Lookup);
    }

    public static bool HasOverrideDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.Override);
    }

    public static bool HasProvidesDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.Provides);
    }

    public static bool HasRequireDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.Require);
    }

    public static bool HasShareableDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.Shareable);
    }
}
