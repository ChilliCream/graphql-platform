using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Completion;

internal static class CompletionTools
{
    public static DirectiveCollection CreateDirectiveCollection(
        ICompositeSchemaContext context,
        IImmutableList<DirectiveNode> directives)
    {
        var temp = new Directive[directives.Count];

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];
            var definition = context.GetDirectiveDefinition(directive.Name.Value);
            var arguments = CreateArgumentAssignments(directive.Arguments);
            temp[i] = new Directive(definition, arguments);
        }

        return new DirectiveCollection(temp);
    }

    private static ArgumentAssignment[] CreateArgumentAssignments(
        IReadOnlyList<ArgumentNode> arguments)
    {
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
        ICompositeSchemaContext context,
        IImmutableList<NamedTypeNode> interfaceTypes)
    {
        var temp = new CompositeInterfaceType[interfaceTypes.Count];

        for (var i = 0; i < interfaceTypes.Count; i++)
        {
            temp[i] = (CompositeInterfaceType)context.GetType(interfaceTypes[i]);
        }
        
        return new CompositeInterfaceTypeCollection(temp);
    }
}
