using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class InputObjectTypeFactory
        : ITypeFactory<InputObjectTypeDefinitionNode, InputObjectType>
    {
        public InputObjectType Create(
            IBindingLookup bindingLookup,
            InputObjectTypeDefinitionNode node)
        {
            if (bindingLookup == null)
            {
                throw new ArgumentNullException(nameof(bindingLookup));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            ITypeBindingInfo bindingInfo =
                bindingLookup.GetBindingInfo(node.Name.Value);

            return new InputObjectType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                if (bindingInfo.SourceType != null)
                {
                    d.Extend().OnBeforeCreate(
                        t => t.ClrType = bindingInfo.SourceType);
                }

                foreach (DirectiveNode directive in node.Directives)
                {
                    d.Directive(directive);
                }

                DeclareFields(bindingInfo, d, node);
            });
        }

        private static void DeclareFields(
            ITypeBindingInfo bindingInfo,
            IInputObjectTypeDescriptor typeDescriptor,
            InputObjectTypeDefinitionNode node)
        {
            foreach (InputValueDefinitionNode inputField in node.Fields)
            {
                bindingInfo.TrackField(inputField.Name.Value);

                IInputFieldDescriptor descriptor = typeDescriptor
                    .Field(inputField.Name.Value)
                    .Description(inputField.Description?.Value)
                    .Type(inputField.Type)
                    .DefaultValue(inputField.DefaultValue)
                    .SyntaxNode(inputField);

                if (bindingInfo.TryGetFieldProperty(
                    inputField.Name.Value,
                    MemberKind.InputObjectField,
                    out PropertyInfo p))
                {
                    descriptor.Extend().OnBeforeCreate(
                        t => t.Property = p);
                }

                foreach (DirectiveNode directive in inputField.Directives)
                {
                    descriptor.Directive(directive);
                }
            }
        }
    }
}
