using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class LookupDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value.Equals("fusion__lookup", StringComparison.Ordinal);

    public static LookupDirective Parse(DirectiveNode directive)
    {
        string? schemaName = null;
        SelectionSetNode? key = null;
        FieldDefinitionNode? field = null;
        ImmutableArray<string>? map = null;

        foreach (var argument in directive.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "schema":
                    schemaName = ((EnumValueNode)argument.Value).Value;
                    break;

                case "key":
                    key = Utf8GraphQLParser.Syntax.ParseSelectionSet(((StringValueNode)argument.Value).Value);
                    break;

                case "field":
                    field = Utf8GraphQLParser.Syntax.ParseFieldDefinition(((StringValueNode)argument.Value).Value);
                    break;

                case "map":
                    map = ParseMap(argument.Value);
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @lookup.");
            }
        }

        if (string.IsNullOrEmpty(schemaName))
        {
            throw new DirectiveParserException(
                "The `schema` argument is required on the @lookup directive.");
        }

        if (key is null)
        {
            throw new DirectiveParserException(
                "The `key` argument is required on the @lookup directive.");
        }

        if (field is null)
        {
            throw new DirectiveParserException(
                "The `field` argument is required on the @lookup directive.");
        }

        if (map is null)
        {
            throw new DirectiveParserException(
                "The `map` argument is required on the @lookup directive.");
        }

        return new LookupDirective(schemaName, key, field, map.Value);
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

    public static ImmutableArray<LookupDirective> Parse(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        ImmutableArray<LookupDirective>.Builder? temp = null;

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                temp ??= ImmutableArray.CreateBuilder<LookupDirective>();
                temp.Add(Parse(directiveNode));
            }
        }

        return temp?.ToImmutable() ?? ImmutableArray<LookupDirective>.Empty;
    }

    public static bool TryParse(
        IReadOnlyList<DirectiveNode> directiveNodes,
        [NotNullWhen(true)] out ImmutableArray<LookupDirective>? directives)
    {
        ImmutableArray<LookupDirective>.Builder? temp = null;

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                temp ??= ImmutableArray.CreateBuilder<LookupDirective>();
                temp.Add(Parse(directiveNode));
            }
        }

        directives = temp?.ToImmutable();
        return temp is not null;
    }
}
