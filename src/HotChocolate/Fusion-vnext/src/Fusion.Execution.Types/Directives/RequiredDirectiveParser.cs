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
        SelectionSetNode? requirements = null;
        FieldDefinitionNode? field = null;
        ImmutableArray<string?>? map = null;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "schema":
                    schemaName = ((EnumValueNode)argument.Value).Value;
                    break;

                case "requirements":
                    var requirementsSourceText = ((StringValueNode)argument.Value).Value.Trim();

                    if (!requirementsSourceText.StartsWith('{'))
                    {
                        requirementsSourceText = $"{{ {requirementsSourceText} }}";
                    }

                    requirements = Utf8GraphQLParser.Syntax.ParseSelectionSet(requirementsSourceText);
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

        if (requirements is null)
        {
            throw new DirectiveParserException(
                "The `requirements` argument is required on the @require directive.");
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

        return new RequireDirective(schemaName, requirements, field, map.Value);
    }

    private static ImmutableArray<string?> ParseMap(IValueNode value)
    {
        switch (value)
        {
            case ListValueNode listValue:
            {
                var fields = ImmutableArray.CreateBuilder<string?>();

                foreach (var item in listValue.Items)
                {
                    if (item is StringValueNode stringValue)
                    {
                        fields.Add(stringValue.Value);
                    }
                    else
                    {
                        fields.Add(null);
                    }
                }

                return fields.ToImmutable();
            }

            case StringValueNode stringValue:
                return [stringValue.Value];

            default:
                throw new DirectiveParserException(
                    "The value is expected to be a list of strings or a string.");
        }
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

        return temp?.ToImmutable() ?? [];
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
