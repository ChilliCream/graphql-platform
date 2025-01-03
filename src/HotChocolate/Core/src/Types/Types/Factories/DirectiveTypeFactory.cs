using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.DirectiveLocationUtils;

namespace HotChocolate.Types.Factories;

internal sealed class DirectiveTypeFactory : ITypeFactory<DirectiveDefinitionNode, DirectiveType>
{
    public DirectiveType Create(IDescriptorContext context, DirectiveDefinitionNode node)
    {
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new DirectiveTypeDefinition(
            node.Name.Value,
            node.Description?.Value,
            isRepeatable: node.IsRepeatable);

        if (context.Options.DefaultDirectiveVisibility is DirectiveVisibility.Public)
        {
            typeDefinition.IsPublic = true;
        }

        DeclareArguments(context, typeDefinition, node.Arguments, path);
        DeclareLocations(typeDefinition, node);

        return DirectiveType.CreateUnsafe(typeDefinition);
    }

    private static void DeclareArguments(
        IDescriptorContext context,
        DirectiveTypeDefinition parent,
        IReadOnlyCollection<InputValueDefinitionNode> arguments,
        Stack<IDefinition> path)
    {
        path.Push(parent);

        foreach (var argument in arguments)
        {
            var argumentDefinition = new DirectiveArgumentDefinition(
                argument.Name.Value,
                argument.Description?.Value,
                TypeReference.Create(argument.Type),
                argument.DefaultValue);

            if (argument.DeprecationReason() is { Length: > 0, } reason)
            {
                argumentDefinition.DeprecationReason = reason;
            }

            SdlToTypeSystemHelper.AddDirectives(context, argumentDefinition, argument, path);

            parent.Arguments.Add(argumentDefinition);
        }

        path.Pop();
    }

    private static void DeclareLocations(
        DirectiveTypeDefinition parent,
        DirectiveDefinitionNode node)
    {
        parent.Locations = Parse(node.Locations);
    }
}
