using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;
using IHasName = HotChocolate.Skimmed.IHasName;

namespace HotChocolate.Fusion.Composition;

internal static class DirectivesHelper
{
    public const string BindDirectiveName = "bind";
    public const string RefDirectiveName = "ref";
    public const string RemoveDirectiveName = "remove";
    public const string ToArg = "to";
    public const string AsArg = "as";
    public const string CoordinateArg = "coordinate";
    public const string FieldArg = "field";

    public static string GetOriginalName<T>(this T member) where T : IHasName, IHasDirectives
        => member.ContainsRefDirective()
            ? member.GetBindDirective().OriginalName ?? member.Name
            : member.Name;

    public static BindDirective GetBindDirective(this IHasDirectives member)
    {
        var directive = member.Directives[RefDirectiveName].First();
        var toArg = directive.Arguments.First(t => t.Name.EqualsOrdinal(ToArg));
        var asArg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(AsArg));

        if (toArg.Value is not StringValueNode toValue)
        {
            throw new InvalidOperationException(
                "The bind argument must have a value for to.");
        }

        return asArg?.Value is StringValueNode asValue
            ? new BindDirective(toValue.Value, asValue.Value)
            : new BindDirective(toValue.Value);
    }

    public static void TryAddBindDirective(
        this IHasDirectives member,
        Schema schema,
        string? originalName = null)
    {
        if (!member.Directives.ContainsName(BindDirectiveName))
        {
            if (originalName is null)
            {
                member.Directives.Add(
                    new Directive(
                        schema.DirectiveTypes[BindDirectiveName],
                        new Argument(ToArg, schema.Name)));
            }
            else
            {
                member.Directives.Add(
                    new Directive(
                        schema.DirectiveTypes[BindDirectiveName],
                        new Argument(ToArg, schema.Name),
                        new Argument(AsArg, originalName)));
            }
        }


    }

    public static void RegisterBindDirective(this Schema schema)
    {
        if (!schema.DirectiveTypes.ContainsName(BindDirectiveName))
        {
            var bind = new DirectiveType(BindDirectiveName);
            bind.Locations = DirectiveLocation.FieldDefinition;
            bind.Arguments.Add(
                new InputField(
                    ToArg,
                    new NonNullType(schema.Types["String"])));
            bind.Arguments.Add(
                new InputField(
                    AsArg,
                    schema.Types["String"]));
            schema.DirectiveTypes.Add(bind);
        }
    }

    public static bool ContainsRefDirective(this IHasDirectives member)
        => member.Directives.ContainsName(RefDirectiveName);

    public static RefDirective GetRefDirective(this IHasDirectives member)
    {
        var directive = member.Directives[RefDirectiveName].First();

        var arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(CoordinateArg));
        if (arg is { Value: StringValueNode coordinate })
        {
            return new RefDirective(SchemaCoordinate.Parse(coordinate.Value));
        }

        arg = directive.Arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(FieldArg));
        if (arg is { Value: StringValueNode field })
        {
            return new RefDirective(Utf8GraphQLParser.Syntax.ParseField(field.Value));
        }

        throw new InvalidOperationException(
            "The ref argument must have a value for coordinate or field.");
    }

    public static IEnumerable<RemoveDirective> GetRemoveDirectives(this IHasDirectives member)
    {
        foreach (var directive in member.Directives[RemoveDirectiveName])
        {
            if (!directive.Arguments.TryGetValue("coordinate", out var value) ||
                value is not StringValueNode coordinateValue)
            {
                throw new InvalidOperationException(
                    "The remove directive must have a value for coordinate.");
            }

            yield return new RemoveDirective(SchemaCoordinate.Parse(coordinateValue.Value));
        }
    }
}
