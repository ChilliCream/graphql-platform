using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class InputObjectTypeExtensionFactory
        : ITypeFactory<InputObjectTypeExtensionNode, InputObjectTypeExtension>
    {
        public InputObjectTypeExtension Create(
            IBindingLookup bindingLookup,
            IReadOnlySchemaOptions schemaOptions,
            InputObjectTypeExtensionNode node)
        {
            if (bindingLookup is null)
            {
                throw new ArgumentNullException(nameof(bindingLookup));
            }

            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            ITypeBindingInfo bindingInfo =
                bindingLookup.GetBindingInfo(node.Name.Value);

            return new InputObjectTypeExtension(d =>
            {
                d.Name(node.Name.Value);

                if (bindingInfo.SourceType != null)
                {
                    d.Extend().OnBeforeCreate(t => t.RuntimeType = bindingInfo.SourceType);
                }

                foreach (DirectiveNode directive in node.Directives)
                {
                    d.Directive(directive);
                }

                DeclareFields(bindingInfo, schemaOptions, d, node);
            });
        }

        private static void DeclareFields(
            ITypeBindingInfo bindingInfo,
            IReadOnlySchemaOptions schemaOptions,
            IInputObjectTypeDescriptor typeDescriptor,
            InputObjectTypeExtensionNode node)
        {
            foreach (InputValueDefinitionNode inputField in node.Fields)
            {
                bindingInfo.TrackField(inputField.Name.Value);

                IInputFieldDescriptor descriptor = typeDescriptor
                    .Field(inputField.Name.Value)
                    .Description(inputField.Description?.Value)
                    .Type(inputField.Type)
                    .SyntaxNode(schemaOptions.KeepSyntaxNodes ? inputField : null);

                if (inputField.DefaultValue is { })
                {
                    descriptor.DefaultValue(inputField.DefaultValue);
                }

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
