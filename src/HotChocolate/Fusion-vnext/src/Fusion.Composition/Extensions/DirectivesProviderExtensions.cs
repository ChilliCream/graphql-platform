using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion.Extensions;

internal static class DirectivesProviderExtensions
{
    public static void AddDirective(this IDirectivesProvider member, Directive directive)
    {
        switch (member)
        {
            case IMutableFieldDefinition field:
                field.Directives.Add(directive);
                break;
            case IMutableTypeDefinition type:
                type.Directives.Add(directive);
                break;
            case MutableEnumValue enumValue:
                enumValue.Directives.Add(directive);
                break;
            case MutableSchemaDefinition schema:
                schema.Directives.Add(directive);
                break;
            default:
                throw new InvalidOperationException();
        }
    }

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

    public static bool ExistsInSchema(this IDirectivesProvider type, string schemaName)
    {
        return type.Directives.AsEnumerable().Any(
            d =>
                d.Name == WellKnownDirectiveNames.FusionType
                && (string)d.Arguments[ArgumentNames.Schema].Value! == schemaName);
    }

    public static bool HasFusionInaccessibleDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.FusionInaccessible);
    }

    public static bool HasInaccessibleDirective(this IDirectivesProvider type)
    {
        return type.Directives.ContainsName(WellKnownDirectiveNames.Inaccessible);
    }
}
