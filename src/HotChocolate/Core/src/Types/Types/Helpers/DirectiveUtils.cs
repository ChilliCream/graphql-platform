using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Helpers;

public static class DirectiveUtils
{
    public static void AddDirective<T>(
        this IHasDirectiveDefinition directivesContainer,
        T directive,
        ITypeInspector typeInspector)
        where T : class
    {
        if (directive is null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        switch (directive)
        {
            case DirectiveNode node:
                directivesContainer.Directives.Add(
                    new DirectiveDefinition(node));
                break;

            case string directiveName:
                AddDirective(
                    directivesContainer,
                    directiveName,
                    Array.Empty<ArgumentNode>());
                break;

            default:
                directivesContainer.Directives.Add(
                    new DirectiveDefinition(
                        directive,
                        TypeReference.CreateDirective(typeInspector.GetType(directive.GetType()))));
                break;
        }
    }

    public static void AddDirective(
        this IHasDirectiveDefinition directivesContainer,
        string name,
        IEnumerable<ArgumentNode> arguments)
    {
        directivesContainer.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    name.EnsureGraphQLName(),
                    arguments.ToArray())));
    }
}
