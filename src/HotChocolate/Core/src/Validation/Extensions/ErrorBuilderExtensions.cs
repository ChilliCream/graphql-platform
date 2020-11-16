using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal static class ErrorBuilderExtensions
    {
        public static IErrorBuilder SpecifiedBy(
            this IErrorBuilder errorBuilder,
            string section) =>
            errorBuilder.SetExtension(
                "specifiedBy",
                "http://spec.graphql.org/June2018/#" + section);

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
}
