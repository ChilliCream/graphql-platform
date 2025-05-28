using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Factories;

internal sealed class UnionTypeFactory
    : ITypeFactory<UnionTypeDefinitionNode, UnionType>
    , ITypeFactory<UnionTypeExtensionNode, UnionTypeExtension>
{
    public UnionType Create(IDescriptorContext context, UnionTypeDefinitionNode node)
    {
        var path = context.GetOrCreateConfigurationStack();
        path.Clear();

        var typeDefinition = new UnionTypeConfiguration(
            node.Name.Value,
            node.Description?.Value)
        {
            BindTo = node.GetBindingValue()
        };

        foreach (var namedType in node.Types)
        {
            typeDefinition.Types.Add(TypeReference.Create(namedType));
        }

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        return UnionType.CreateUnsafe(typeDefinition);
    }

    public UnionTypeExtension Create(IDescriptorContext context, UnionTypeExtensionNode node)
    {
        var path = context.GetOrCreateConfigurationStack();
        path.Clear();

        var typeDefinition = new UnionTypeConfiguration(node.Name.Value);
        typeDefinition.BindTo = node.GetBindingValue();

        foreach (var namedType in node.Types)
        {
            typeDefinition.Types.Add(TypeReference.Create(namedType));
        }

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        return UnionTypeExtension.CreateUnsafe(typeDefinition);
    }
}
