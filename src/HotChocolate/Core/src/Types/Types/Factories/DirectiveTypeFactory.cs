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
        var typeDefinition = new DirectiveTypeDefinition(
            node.Name.Value,
            node.Description?.Value,
            isRepeatable: node.IsRepeatable);

        if (context.Options.DefaultDirectiveVisibility is DirectiveVisibility.Public)
        {
            typeDefinition.IsPublic = true;
        }

        DeclareArguments(typeDefinition, node.Arguments);
        DeclareLocations(typeDefinition, node);

        return DirectiveType.CreateUnsafe(typeDefinition);
    }

    private static void DeclareArguments(
        DirectiveTypeDefinition parent,
        IReadOnlyCollection<InputValueDefinitionNode> arguments)
    {
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

            parent.Arguments.Add(argumentDefinition);
        }
    }

    private static void DeclareLocations(
        DirectiveTypeDefinition parent,
        DirectiveDefinitionNode node)
    {
        parent.Locations = Parse(node.Locations);
    }
}
