using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class RequiredDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value.Equals("fusion__requires", StringComparison.Ordinal);

    public static RequireDirective Parse(DirectiveNode directiveNode)
    {
        string? schemaName = null;
        FieldDefinitionNode? field = null;
        ImmutableArray<string>? map = null;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "schema":
                    schemaName = ((EnumValueNode)argument.Value).Value;
                    break;

                case "field":
                    field = Utf8GraphQLParser.Syntax.ParseFieldDefinition(((StringValueNode)argument.Value).Value);
                    break;

                case "map":
                    map = ParseMap(argument.Value);
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @require.");
            }
        }

        if (string.IsNullOrEmpty(schemaName))
        {
            throw new DirectiveParserException(
                "The `schema` argument is required on the @require directive.");
        }

        if (field is null)
        {
            throw new DirectiveParserException(
                "The `field` argument is required on the @require directive.");
        }

        if (map is null)
        {
            throw new DirectiveParserException(
                "The `map` argument is required on the @require directive.");
        }

        return new RequireDirective(schemaName, field, map.Value);
    }

    private static ImmutableArray<string> ParseMap(IValueNode value)
    {
        if (value is ListValueNode listValue)
        {
            var fields = ImmutableArray.CreateBuilder<string>();

            foreach (var item in listValue.Items)
            {
                fields.Add(((StringValueNode)item).Value);
            }

            return fields.ToImmutable();
        }

        if (value is StringValueNode stringValue)
        {
            return ImmutableArray<string>.Empty.Add(stringValue.Value);
        }

        throw new DirectiveParserException(
            "The value is expected to be a list of strings or a string.");
    }

    public static ImmutableArray<RequireDirective> Parse(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        ImmutableArray<RequireDirective>.Builder? temp = null;

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                temp ??= ImmutableArray.CreateBuilder<RequireDirective>();
                temp.Add(Parse(directiveNode));
            }
        }

        return temp?.ToImmutable() ?? ImmutableArray<RequireDirective>.Empty;
    }

    public static bool TryParse(
        IReadOnlyList<DirectiveNode> directiveNodes,
        [NotNullWhen(true)] out ImmutableArray<RequireDirective>? directives)
    {
        ImmutableArray<RequireDirective>.Builder? temp = null;

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                temp ??= ImmutableArray.CreateBuilder<RequireDirective>();
                temp.Add(Parse(directiveNode));
            }
        }

        directives = temp?.ToImmutable();
        return temp is not null;
    }
}
