using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories;

internal sealed class EnumTypeFactory
    : ITypeFactory<EnumTypeDefinitionNode, EnumType>
    , ITypeFactory<EnumTypeExtensionNode, EnumTypeExtension>
{
    public EnumType Create(IDescriptorContext context, EnumTypeDefinitionNode node)
    {
        var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new EnumTypeDefinition(
            node.Name.Value,
            node.Description?.Value);
        typeDefinition.BindTo = node.GetBindingValue();

        if (preserveSyntaxNodes)
        {
            typeDefinition.SyntaxNode = node;
        }

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        DeclareValues(context, typeDefinition, node.Values, path, preserveSyntaxNodes);

        return EnumType.CreateUnsafe(typeDefinition);
    }

    public EnumTypeExtension Create(IDescriptorContext context, EnumTypeExtensionNode node)
    {
        var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new EnumTypeDefinition(node.Name.Value);
        typeDefinition.BindTo = node.GetBindingValue();

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        DeclareValues(context, typeDefinition, node.Values, path, preserveSyntaxNodes);

        return EnumTypeExtension.CreateUnsafe(typeDefinition);
    }

    private static void DeclareValues(
        IDescriptorContext context,
        EnumTypeDefinition parent,
        IReadOnlyCollection<EnumValueDefinitionNode> values,
        Stack<IDefinition> path,
        bool preserveSyntaxNodes)
    {
        path.Push(parent);

        foreach (var value in values)
        {
            var valueDefinition = new EnumValueDefinition(
                value.Name.Value,
                value.Description?.Value,
                value.Name.Value);
            valueDefinition.BindTo = value.GetBindingValue();

            if (preserveSyntaxNodes)
            {
                valueDefinition.SyntaxNode = value;
            }

            SdlToTypeSystemHelper.AddDirectives(context, valueDefinition, value, path);

            if (value.DeprecationReason() is { Length: > 0, } reason)
            {
                valueDefinition.DeprecationReason = reason;
            }

            parent.Values.Add(valueDefinition);
        }

        path.Pop();
    }
}
