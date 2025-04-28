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
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new InputObjectTypeConfiguration(
            node.Name.Value,
            node.Description?.Value)
        {
            BindTo = node.GetBindingValue()
        };

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        DeclareFields(context, typeDefinition, node.Fields, path);

        return InputObjectType.CreateUnsafe(typeDefinition);
    }

    public InputObjectTypeExtension Create(IDescriptorContext context, InputObjectTypeExtensionNode node)
    {
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new InputObjectTypeConfiguration(node.Name.Value)
        {
            BindTo = node.GetBindingValue()
        };

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        DeclareFields(context, typeDefinition, node.Fields, path);

        return InputObjectTypeExtension.CreateUnsafe(typeDefinition);
    }

    private static void DeclareFields(
        IDescriptorContext context,
        InputObjectTypeConfiguration parent,
        IReadOnlyCollection<InputValueDefinitionNode> fields,
        Stack<ITypeSystemConfiguration> path)
    {
        path.Push(parent);

        foreach (var inputField in fields)
        {
            var inputFieldDefinition = new InputFieldConfiguration(
                inputField.Name.Value,
                inputField.Description?.Value,
                TypeReference.Create(inputField.Type),
                inputField.DefaultValue)
            {
                BindTo = inputField.GetBindingValue()
            };

            if (inputField.DeprecationReason() is { Length: > 0 } reason)
            {
                inputFieldDefinition.DeprecationReason = reason;
            }

            SdlToTypeSystemHelper.AddDirectives(context, inputFieldDefinition, inputField, path);

            parent.Fields.Add(inputFieldDefinition);
        }

        path.Pop();
    }
}
