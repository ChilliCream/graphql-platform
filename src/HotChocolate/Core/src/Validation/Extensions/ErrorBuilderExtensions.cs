using HotChocolate.Language;

namespace HotChocolate.Validation;

internal static class ErrorBuilderExtensions
{
    public static IErrorBuilder SpecifiedBy(
        this IErrorBuilder errorBuilder,
        string section,
        bool isDraft = false,
        int? rfc = null)
    {
        if (isDraft || rfc.HasValue)
        {
            errorBuilder.SetExtension(
                "specifiedBy",
                "http://spec.graphql.org/draft/#" + section);

            if (rfc.HasValue)
            {
                errorBuilder.SetExtension(
                    "rfc",
                    "https://github.com/graphql/graphql-spec/pull/" + rfc.Value);
            }
        }
        else
        {
            errorBuilder.SetExtension(
                "specifiedBy",
                "http://spec.graphql.org/October2021/#" + section);
        }

        return errorBuilder;
    }

    public static IErrorBuilder SetFragmentName(
        this IErrorBuilder errorBuilder,
        ISyntaxNode node)
    {
        if (node.Kind == SyntaxKind.FragmentDefinition)
        {
            errorBuilder.SetExtension("fragment", ((FragmentDefinitionNode)node).Name.Value);
        }
        return errorBuilder;
    }
}
