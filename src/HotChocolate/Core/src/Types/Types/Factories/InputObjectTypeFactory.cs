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
            IReadOnlySchemaOptions schemaOptions,
            InputObjectTypeDefinitionNode node)
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

            return new InputObjectType(d =>
            {
                d.SyntaxNode(schemaOptions.KeepSyntaxNodes ? node : null)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

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
            InputObjectTypeDefinitionNode node)
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
