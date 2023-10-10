using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Composition.Properties.CompositionResources;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition;

internal static class DirectivesHelper
{
    public const string CoordinateArg = "coordinate";
    public const string NewNameArg = "newName";
    public const string FieldArg = "field";

    public static bool ContainsIsDirective(this IHasDirectives member, FusionTypes types)
        => member.Directives.ContainsName(types.Is.Name);
    
    public static IsDirective GetIsDirective(this IHasDirectives member, FusionTypes types)
    {
        var directive = member.Directives[types.Is.Name].First();

        var arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(CoordinateArg));

        if (arg is { Value: StringValueNode coordinate })
        {
            return new IsDirective(SchemaCoordinate.Parse(coordinate.Value));
        }

        arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(FieldArg));

        if (arg is { Value: StringValueNode field })
        {
            return new IsDirective(Utf8GraphQLParser.Syntax.ParseField(field.Value));
        }

        throw new InvalidOperationException(
            DirectivesHelper_GetIsDirective_NoFieldAndNoCoordinate);
    }

    public static bool ContainsRequireDirective(this IHasDirectives member, FusionTypes types)
        => member.Directives.ContainsName(types.Require.Name);

    public static RequireDirective GetRequireDirective(this IHasDirectives member, FusionTypes types)
    {
        var directive = member.Directives[types.Require.Name].First();
        var arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(FieldArg));

        if (arg is { Value: StringValueNode field })
        {
            return new RequireDirective(Utf8GraphQLParser.Syntax.ParseField(field.Value));
        }

        throw new InvalidOperationException(
            DirectivesHelper_GetRequireDirective_NoFieldArg);
    }
    
    public static bool ContainsResolveDirective(this IHasDirectives member, FusionTypes types)
        => member.Directives.ContainsName(types.Resolve.Name);

    public static IEnumerable<DeclareDirective> GetRequireDirective(this OutputField member, FusionTypes types)
    {
        foreach (var directive in member.Directives[types.Declare.Name])
        {
            
            yield return new DeclareDirective()
        }
        var arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(FieldArg));

        if (arg is { Value: StringValueNode field })
        {
            return new RequireDirective(Utf8GraphQLParser.Syntax.ParseField(field.Value));
        }

        throw new InvalidOperationException(
            DirectivesHelper_GetRequireDirective_NoFieldArg);
    }
}
