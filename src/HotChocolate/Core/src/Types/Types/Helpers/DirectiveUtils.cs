using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Helpers;

public static class DirectiveUtils
{
    public static void AddDirective<T>(
        this IDirectiveConfigurationProvider directivesContainer,
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
                    new DirectiveConfiguration(node));
                break;

            case string directiveName:
                AddDirective(
                    directivesContainer,
                    directiveName,
                    Array.Empty<ArgumentNode>());
                break;

            default:
                directivesContainer.Directives.Add(
                    new DirectiveConfiguration(
                        directive,
                        TypeReference.CreateDirective(typeInspector.GetType(directive.GetType()))));
                break;
        }
    }

    public static void AddDirective(
        this IDirectiveConfigurationProvider directivesContainer,
        string name,
        IEnumerable<ArgumentNode> arguments)
    {
        directivesContainer.Directives.Add(
            new DirectiveConfiguration(
                new DirectiveNode(
                    name.EnsureGraphQLName(),
                    [.. arguments])));
    }
}
