using HotChocolate.Skimmed;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class DirectivesProviderExtensions
{
    public static bool HasExternalDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(External);
    }

    public static bool HasInaccessibleDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(Inaccessible);
    }

    public static bool HasInternalDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(Internal);
    }

    public static bool HasLookupDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(Lookup);
    }

    public static bool HasOverrideDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(Override);
    }

    public static bool HasProvidesDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(Provides);
    }
}
