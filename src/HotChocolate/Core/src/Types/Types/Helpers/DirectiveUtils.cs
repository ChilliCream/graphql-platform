using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

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
            case string s:
                AddDirective(
                    directivesContainer,
                    new NameString(s),
                    Array.Empty<ArgumentNode>());
                break;
            default:
                directivesContainer.Directives.Add(
                    new DirectiveDefinition(
                        directive,
                        typeInspector.GetTypeRef(directive.GetType(), TypeContext.None)));
                break;
        }
    }

    public static void AddDirective(
        this IHasDirectiveDefinition directivesContainer,
        NameString name,
        IEnumerable<ArgumentNode> arguments)
    {
        directivesContainer.Directives.Add(
            new DirectiveDefinition(
                new DirectiveNode(
                    name.EnsureNotEmpty(nameof(name)),
                    arguments.ToArray())));
    }
}
