using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class ObjectTypeFactory
        : ITypeFactory<ObjectTypeDefinitionNode, ObjectType>
    {
        public ObjectType Create(
            IBindingLookup bindingLookup,
            ObjectTypeDefinitionNode node)
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

            return new ObjectType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                if (bindingInfo.SourceType != null)
                {
                    d.Type(bindingInfo.SourceType);
                }

                foreach (DirectiveNode directive in node.Directives)
                {
                    d.Directive(directive);
                }

                DeclareInterfaces(d, node.Interfaces);

                DeclareFields(bindingInfo, d, node.Fields);
            });
        }

        private static void DeclareInterfaces(
            IObjectTypeDescriptor typeDescriptor,
            IReadOnlyCollection<NamedTypeNode> interfaceReferences)
        {
            foreach (NamedTypeNode typeNode in interfaceReferences)
            {
                typeDescriptor.Interface(typeNode);
            }
        }

        private static void DeclareFields(
            ITypeBindingInfo bindingLookup,
            IObjectTypeDescriptor typeDescriptor,
            IReadOnlyCollection<FieldDefinitionNode> fieldDefinitions)
        {
            foreach (FieldDefinitionNode fieldDefinition in fieldDefinitions)
            {
                bindingLookup.TrackField(fieldDefinition.Name.Value);

                IObjectFieldDescriptor fieldDescriptor = typeDescriptor
                    .Field(fieldDefinition.Name.Value)
                    .Description(fieldDefinition.Description?.Value)
                    .Type(fieldDefinition.Type)
                    .SyntaxNode(fieldDefinition);

                foreach (DirectiveNode directive in fieldDefinition.Directives)
                {
                    fieldDescriptor.Directive(directive);
                }

                string deprecactionReason = fieldDefinition.DeprecationReason();
                if (!string.IsNullOrEmpty(deprecactionReason))
                {
                    fieldDescriptor.DeprecationReason(deprecactionReason);
                }

                DeclareFieldArguments(fieldDescriptor, fieldDefinition);
            }
        }

        private static void DeclareFieldArguments(
            IObjectFieldDescriptor fieldDescriptor,
            FieldDefinitionNode fieldDefinition)
        {
            foreach (InputValueDefinitionNode inputFieldDefinition in
                fieldDefinition.Arguments)
            {
                fieldDescriptor.Argument(inputFieldDefinition.Name.Value,
                    a =>
                    {
                        IArgumentDescriptor descriptor = a
                            .Description(
                                inputFieldDefinition.Description?.Value)
                            .Type(inputFieldDefinition.Type)
                            .DefaultValue(inputFieldDefinition.DefaultValue)
                            .SyntaxNode(inputFieldDefinition);

                        foreach (DirectiveNode directive in
                            inputFieldDefinition.Directives)
                        {
                            descriptor.Directive(directive);
                        }
                    });
            }
        }
    }
}
