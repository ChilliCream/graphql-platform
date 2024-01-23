using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Factories;

internal sealed class InterfaceTypeFactory
    : ITypeFactory<InterfaceTypeDefinitionNode, InterfaceType>
    , ITypeFactory<InterfaceTypeExtensionNode, InterfaceTypeExtension>
{
    public InterfaceType Create(IDescriptorContext context, InterfaceTypeDefinitionNode node)
    {
        var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new InterfaceTypeDefinition(
            node.Name.Value,
            node.Description?.Value);
        typeDefinition.BindTo = node.GetBindingValue();

        if (preserveSyntaxNodes)
        {
            typeDefinition.SyntaxNode = node;
        }

        foreach (var typeNode in node.Interfaces)
        {
            typeDefinition.Interfaces.Add(TypeReference.Create(typeNode));
        }

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        DeclareFields(context, typeDefinition, node.Fields, path, preserveSyntaxNodes);

        return InterfaceType.CreateUnsafe(typeDefinition);
    }

    public InterfaceTypeExtension Create(IDescriptorContext context, InterfaceTypeExtensionNode node)
    {
        var preserveSyntaxNodes = context.Options.PreserveSyntaxNodes;
        var path = context.GetOrCreateDefinitionStack();
        path.Clear();

        var typeDefinition = new InterfaceTypeDefinition(node.Name.Value);
        typeDefinition.BindTo = node.GetBindingValue();

        foreach (var typeNode in node.Interfaces)
        {
            typeDefinition.Interfaces.Add(TypeReference.Create(typeNode));
        }

        SdlToTypeSystemHelper.AddDirectives(context, typeDefinition, node, path);

        DeclareFields(context, typeDefinition, node.Fields, path, preserveSyntaxNodes);

        return InterfaceTypeExtension.CreateUnsafe(typeDefinition);
    }

    private static void DeclareFields(
        IDescriptorContext context,
        InterfaceTypeDefinition parent,
        IReadOnlyCollection<FieldDefinitionNode> fields,
        Stack<IDefinition> path,
        bool preserveSyntaxNodes)
    {
        path.Push(parent);

        foreach (var field in fields)
        {
            var fieldDefinition = new InterfaceFieldDefinition(
                field.Name.Value,
                field.Description?.Value,
                TypeReference.Create(field.Type));
            fieldDefinition.BindTo = field.GetBindingValue();

            if (preserveSyntaxNodes)
            {
                fieldDefinition.SyntaxNode = field;
            }

            SdlToTypeSystemHelper.AddDirectives(context, fieldDefinition, field, path);

            if (field.DeprecationReason() is { Length: > 0, } reason)
            {
                fieldDefinition.DeprecationReason = reason;
            }

            DeclareFieldArguments(context, fieldDefinition, field, path, preserveSyntaxNodes);

            parent.Fields.Add(fieldDefinition);
        }

        path.Pop();
    }

    private static void DeclareFieldArguments(
        IDescriptorContext context,
        InterfaceFieldDefinition parent,
        FieldDefinitionNode field,
        Stack<IDefinition> path,
        bool preserveSyntaxNodes)
    {
        path.Push(parent);

        foreach (var argument in field.Arguments)
        {
            var argumentDefinition = new ArgumentDefinition(
                argument.Name.Value,
                argument.Description?.Value,
                TypeReference.Create(argument.Type),
                argument.DefaultValue);
            argumentDefinition.BindTo = argument.GetBindingValue();

            if (preserveSyntaxNodes)
            {
                argumentDefinition.SyntaxNode = argument;
            }

            if (argument.DeprecationReason() is { Length: > 0, } reason)
            {
                argumentDefinition.DeprecationReason = reason;
            }

            SdlToTypeSystemHelper.AddDirectives(context, argumentDefinition, argument, path);

            parent.Arguments.Add(argumentDefinition);
        }

        path.Pop();
    }
}
