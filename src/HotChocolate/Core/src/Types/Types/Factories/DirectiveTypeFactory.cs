using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Types.DirectiveLocationUtils;

namespace HotChocolate.Types.Factories;

internal sealed class DirectiveTypeFactory : ITypeFactory<DirectiveDefinitionNode, DirectiveType>
{
    public DirectiveType Create(IDescriptorContext context, DirectiveDefinitionNode node)
    {
        var path = context.GetOrCreateConfigurationStack();
        path.Clear();

        var typeDefinition = new DirectiveTypeConfiguration(
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
        DirectiveTypeConfiguration parent,
        IReadOnlyCollection<InputValueDefinitionNode> arguments,
        Stack<ITypeSystemConfiguration> path)
    {
        path.Push(parent);

        foreach (var argument in arguments)
        {
            var argumentDefinition = new DirectiveArgumentConfiguration(
                argument.Name.Value,
                argument.Description?.Value,
                TypeReference.Create(argument.Type),
                argument.DefaultValue);

            if (argument.DeprecationReason() is { Length: > 0 } reason)
            {
                argumentDefinition.DeprecationReason = reason;
            }

            SdlToTypeSystemHelper.AddDirectives(context, argumentDefinition, argument, path);

            parent.Arguments.Add(argumentDefinition);
        }

        path.Pop();
    }

    private static void DeclareLocations(
        DirectiveTypeConfiguration parent,
        DirectiveDefinitionNode node)
    {
        parent.Locations = Parse(node.Locations);
    }
}
