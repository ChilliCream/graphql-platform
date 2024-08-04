using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Fusion.Planning.Directives;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Completion;

internal static class CompletionTools
{
    public static DirectiveCollection CreateDirectiveCollection(
        IReadOnlyList<DirectiveNode> directives,
        CompositeSchemaContext context)
    {
        directives = DirectiveTools.GetUserDirectives(directives);

        if(directives.Count == 0)
        {
            return DirectiveCollection.Empty;
        }

        var temp = new CompositeDirective[directives.Count];

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];
            var definition = context.GetDirectiveDefinition(directive.Name.Value);
            var arguments = CreateArgumentAssignments(directive.Arguments);
            temp[i] = new CompositeDirective(definition, arguments);
        }

        return new DirectiveCollection(temp);
    }

    private static ArgumentAssignment[] CreateArgumentAssignments(
        IReadOnlyList<ArgumentNode> arguments)
    {
        if(arguments.Count == 0)
        {
            return [];
        }

        var assignments = new ArgumentAssignment[arguments.Count];

        for (var i = 0; i < arguments.Count; i++)
        {
            assignments[i] = CreateArgumentAssignment(arguments[i]);
        }

        return assignments;
    }

    private static ArgumentAssignment CreateArgumentAssignment(
        ArgumentNode argument)
        => new(argument.Name.Value, argument.Value);

    public static CompositeInterfaceTypeCollection CreateInterfaceTypeCollection(
        IReadOnlyList<NamedTypeNode> interfaceTypes,
        CompositeSchemaContext context)
    {
        if(interfaceTypes.Count == 0)
        {
            return CompositeInterfaceTypeCollection.Empty;
        }

        var temp = new CompositeInterfaceType[interfaceTypes.Count];

        for (var i = 0; i < interfaceTypes.Count; i++)
        {
            temp[i] = (CompositeInterfaceType)context.GetType(interfaceTypes[i]);
        }

        return new CompositeInterfaceTypeCollection(temp);
    }
}
