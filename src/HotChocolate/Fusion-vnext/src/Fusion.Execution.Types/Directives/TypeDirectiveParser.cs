using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class TypeDirectiveParser
{
    public static bool CanParse(DirectiveNode directiveNode)
        => directiveNode.Name.Value.Equals("fusion__type", StringComparison.Ordinal);

    public static TypeDirective Parse(DirectiveNode directiveNode)
    {
        string? schemaKey = null;

        foreach (var argument in directiveNode.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "schema":
                    schemaKey = ((EnumValueNode)argument.Value).Value;
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @type.");
            }
        }

        if (string.IsNullOrEmpty(schemaKey))
        {
            throw new DirectiveParserException(
                "The `schema` argument is required on the @type directive.");
        }

        return new TypeDirective(new SchemaKey(schemaKey));
    }

    public static ImmutableArray<TypeDirective> Parse(
        IReadOnlyList<DirectiveNode> directiveNodes)
    {
        ImmutableArray<TypeDirective>.Builder? temp = null;

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                temp ??= ImmutableArray.CreateBuilder<TypeDirective>();
                temp.Add(Parse(directiveNode));
            }
        }

        return temp?.ToImmutable() ?? [];
    }

    public static bool TryParse(
        IReadOnlyList<DirectiveNode> directiveNodes,
        [NotNullWhen(true)] out ImmutableArray<TypeDirective>? directives)
    {
        ImmutableArray<TypeDirective>.Builder? temp = null;

        for (var i = 0; i < directiveNodes.Count; i++)
        {
            var directiveNode = directiveNodes[i];
            if (CanParse(directiveNode))
            {
                temp ??= ImmutableArray.CreateBuilder<TypeDirective>();
                temp.Add(Parse(directiveNode));
            }
        }

        directives = temp?.ToImmutable();
        return temp is not null;
    }
}
