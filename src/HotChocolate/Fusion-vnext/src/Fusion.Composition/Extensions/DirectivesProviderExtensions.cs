using HotChocolate.Language;
using HotChocolate.Skimmed;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class DirectivesProviderExtensions
{
    public static string? GetIsFieldSelectionMap(this IDirectivesProvider type)
    {
        var isDirective = type.Directives.FirstOrDefault(d => d.Name == DirectiveNames.Is);

        if (isDirective?.Arguments[ArgumentNames.Field] is StringValueNode fieldArgument)
        {
            return fieldArgument.Value;
        }

        return null;
    }

    public static string? GetProvidesSelectionSet(this IDirectivesProvider type)
    {
        var providesDirective =
            type.Directives.FirstOrDefault(d => d.Name == DirectiveNames.Provides);

        if (providesDirective?.Arguments[ArgumentNames.Fields] is StringValueNode fieldsArgument)
        {
            return fieldsArgument.Value;
        }

        return null;
    }

    public static bool HasExternalDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(DirectiveNames.External);
    }

    public static bool HasInaccessibleDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(DirectiveNames.Inaccessible);
    }

    public static bool HasInternalDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(DirectiveNames.Internal);
    }

    public static bool HasLookupDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(DirectiveNames.Lookup);
    }

    public static bool HasOverrideDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(DirectiveNames.Override);
    }

    public static bool HasProvidesDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(DirectiveNames.Provides);
    }

    public static bool HasRequireDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(DirectiveNames.Require);
    }
}
