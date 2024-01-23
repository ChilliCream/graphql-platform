using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories;

internal sealed class InputObjectTypeFactory
    : ITypeFactory<InputObjectTypeDefinitionNode, InputObjectType>
    , ITypeFactory<InputObjectTypeExtensionNode, InputObjectTypeExtension>
{
    public InputObjectType Create(
        IDescriptorContext context,
        InputObjectTypeDefinitionNode node)
    {
        var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new InputObjectTypeDefinition(
            node.Name.Value,
            node.Description?.Value);
        typeDefinition.BindTo = node.GetBindingValue();

        if (preserveSyntaxNodes)
        {
            typeDefinition.SyntaxNode = node;
        }

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        DeclareFields(context, typeDefinition, node.Fields, path, preserveSyntaxNodes);

        return InputObjectType.CreateUnsafe(typeDefinition);
    }

    public InputObjectTypeExtension Create(IDescriptorContext context, InputObjectTypeExtensionNode node)
    {
        var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new InputObjectTypeDefinition(node.Name.Value);
        typeDefinition.BindTo = node.GetBindingValue();

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        DeclareFields(context, typeDefinition, node.Fields, path, preserveSyntaxNodes);

        return InputObjectTypeExtension.CreateUnsafe(typeDefinition);
    }

    private static void DeclareFields(
        IDescriptorContext context,
        InputObjectTypeDefinition parent,
        IReadOnlyCollection<InputValueDefinitionNode> fields,
        Stack<IDefinition> path,
        bool preserveSyntaxNodes)
    {
        path.Push(parent);

        foreach (var inputField in fields)
        {
            var inputFieldDefinition = new InputFieldDefinition(
                inputField.Name.Value,
                inputField.Description?.Value,
                TypeReference.Create(inputField.Type),
                inputField.DefaultValue);
            inputFieldDefinition.BindTo = inputField.GetBindingValue();

            if (preserveSyntaxNodes)
            {
                inputFieldDefinition.SyntaxNode = inputField;
            }

            if (inputField.DeprecationReason() is { Length: > 0, } reason)
            {
                inputFieldDefinition.DeprecationReason = reason;
            }

            SdlToTypeSystemHelper.AddDirectives(context, inputFieldDefinition, inputField, path);

            parent.Fields.Add(inputFieldDefinition);
        }

        path.Pop();
    }
}
