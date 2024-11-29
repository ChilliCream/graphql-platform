using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class FieldDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value.Equals("fusion__field", StringComparison.Ordinal);

    public static FieldDirective Parse(DirectiveNode directive)
    {
        string? schemaName = null;
        string? sourceName = null;
        ITypeNode? sourceType = null;
        SelectionSetNode? provides = null;
        var isExternal = false;

        foreach (var argument in directive.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "schema":
                    schemaName = ((EnumValueNode)argument.Value).Value;
                    break;

                case "sourceName":
                    sourceName = ((StringValueNode)argument.Value).Value;
                    break;

                case "sourceType":
                    sourceType = Utf8GraphQLParser.Syntax.ParseTypeReference(((StringValueNode)argument.Value).Value);
                    break;

                case "provides":
                    provides = Utf8GraphQLParser.Syntax.ParseSelectionSet(((StringValueNode)argument.Value).Value);
                    break;

                case "external":
                    isExternal = ((BooleanValueNode)argument.Value).Value;
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @field.");
            }
        }

        if (string.IsNullOrEmpty(schemaName))
        {
            throw new DirectiveParserException(
                "The `schema` argument is required on the @field directive.");
        }

        return new FieldDirective(schemaName, sourceName, sourceType, provides, isExternal);
    }

    public static ImmutableArray<FieldDirective> Parse(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        ImmutableArray<FieldDirective>.Builder? temp = null;

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                temp ??= ImmutableArray.CreateBuilder<FieldDirective>();
                temp.Add(Parse(directiveNode));
            }
        }

        return temp?.ToImmutable() ?? ImmutableArray<FieldDirective>.Empty;
    }

    public static bool TryParse(
        IReadOnlyList<DirectiveNode> directiveNodes,
        [NotNullWhen(true)] out ImmutableArray<FieldDirective>? directives)
    {
        ImmutableArray<FieldDirective>.Builder? temp = null;

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                temp ??= ImmutableArray.CreateBuilder<FieldDirective>();
                temp.Add(Parse(directiveNode));
            }
        }

        directives = temp?.ToImmutable();
        return temp is not null;
    }
}
