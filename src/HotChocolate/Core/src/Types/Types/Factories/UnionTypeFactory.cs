using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories;

internal sealed class UnionTypeFactory
    : ITypeFactory<UnionTypeDefinitionNode, UnionType>
    , ITypeFactory<UnionTypeExtensionNode, UnionTypeExtension>
{
    public UnionType Create(IDescriptorContext context, UnionTypeDefinitionNode node)
    {
        var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new UnionTypeDefinition(node.Name.Value, node.Description?.Value);

        typeDefinition.BindTo = node.GetBindingValue();

        if (preserveSyntaxNodes)
        {
            typeDefinition.SyntaxNode = node;
        }

        foreach (var namedType in node.Types)
        {
            typeDefinition.Types.Add(TypeReference.Create(namedType));
        }

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        return UnionType.CreateUnsafe(typeDefinition);
    }

    public UnionTypeExtension Create(IDescriptorContext context, UnionTypeExtensionNode node)
    {
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new UnionTypeDefinition(node.Name.Value);
        typeDefinition.BindTo = node.GetBindingValue();

        foreach (var namedType in node.Types)
        {
            typeDefinition.Types.Add(TypeReference.Create(namedType));
        }

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        return UnionTypeExtension.CreateUnsafe(typeDefinition);
    }
}
