using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Composition.Properties.CompositionResources;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition;

internal static class DirectivesHelper
{
    public const string IsDirectiveName = "is";
    public const string RequireDirectiveName = "require";
    public const string RemoveDirectiveName = "remove";
    public const string RenameDirectiveName = "rename";
    public const string CoordinateArg = "coordinate";
    public const string NewNameArg = "newName";
    public const string FieldArg = "field";

    public static bool ContainsIsDirective(this IHasDirectives member)
        => member.Directives.ContainsName(IsDirectiveName);

    public static IsDirective GetIsDirective(this IHasDirectives member)
    {
        var directive = member.Directives[IsDirectiveName].First();

        var arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(CoordinateArg));

        if (arg is { Value: StringValueNode coordinate, })
        {
            return new IsDirective(SchemaCoordinate.Parse(coordinate.Value));
        }

        arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(FieldArg));

        if (arg is { Value: StringValueNode field, })
        {
            return new IsDirective(Utf8GraphQLParser.Syntax.ParseField(field.Value));
        }

        throw new InvalidOperationException(
            DirectivesHelper_GetIsDirective_NoFieldAndNoCoordinate);
    }

    public static bool ContainsRequireDirective(this IHasDirectives member)
        => member.Directives.ContainsName(RequireDirectiveName);

    public static RequireDirective GetRequireDirective(this IHasDirectives member)
    {
        var directive = member.Directives[RequireDirectiveName].First();
        var arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(FieldArg));

        if (arg is { Value: StringValueNode field, })
        {
            return new RequireDirective(Utf8GraphQLParser.Syntax.ParseField(field.Value));
        }

        throw new InvalidOperationException(
            DirectivesHelper_GetRequireDirective_NoFieldArg);
    }
}
