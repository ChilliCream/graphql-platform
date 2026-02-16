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
        string? schemaKey = null;
        string? sourceName = null;
        ITypeNode? sourceType = null;
        SelectionSetNode? provides = null;
        var isExternal = false;

        foreach (var argument in directive.Arguments)
        {
            switch (argument.Name.Value)
            {
                case "schema":
                    schemaKey = ((EnumValueNode)argument.Value).Value;
                    break;

                case "sourceName":
                    sourceName = ((StringValueNode)argument.Value).Value;
                    break;

                case "sourceType":
                    sourceType = Utf8GraphQLParser.Syntax.ParseTypeReference(((StringValueNode)argument.Value).Value);
                    break;

                case "provides":
                    var providesValue = ((StringValueNode)argument.Value).Value;
                    provides = ParseProvidesSelectionSet(providesValue);
                    break;

                case "external":
                    isExternal = ((BooleanValueNode)argument.Value).Value;
                    break;

                case "partial":
                    // `partial` is the composition-time encoding for external source fields.
                    isExternal = ((BooleanValueNode)argument.Value).Value;
                    break;

                default:
                    throw new DirectiveParserException(
                        $"The argument `{argument.Name.Value}` is not supported on @field.");
            }
        }

        if (string.IsNullOrEmpty(schemaKey))
        {
            throw new DirectiveParserException(
                "The `schema` argument is required on the @field directive.");
        }

        return new FieldDirective(new SchemaKey(schemaKey), sourceName, sourceType, provides, isExternal);
    }

    private static SelectionSetNode ParseProvidesSelectionSet(string value)
    {
        try
        {
            // Fusion schemas can encode provides either as a raw selection set (`{ id }`) or as
            // the legacy field-set form (`id`).
            return Utf8GraphQLParser.Syntax.ParseSelectionSet(value);
        }
        catch (SyntaxException)
        {
            return Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{ {value} }}");
        }
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

        return temp?.ToImmutable() ?? [];
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
