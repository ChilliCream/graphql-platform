using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Types.Factories
{
    internal sealed class ObjectTypeFactory
        : ITypeFactory<ObjectTypeDefinitionNode, ObjectType>
    {
        public ObjectType Create(ObjectTypeDefinitionNode node)
        {
            return new ObjectType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                foreach (DirectiveNode directive in node.Directives)
                {
                    d.Directive(directive);
                }

                DeclareInterfaces(d, node.Interfaces);

                DeclareFields(d, node.Fields);
            });
        }

        private void DeclareInterfaces(
            IObjectTypeDescriptor typeDescriptor,
            IReadOnlyCollection<NamedTypeNode> interfaceReferences)
        {
            foreach (NamedTypeNode typeNode in interfaceReferences)
            {
                typeDescriptor.Interface(typeNode);
            }
        }

        private void DeclareFields(
            IObjectTypeDescriptor typeDescriptor,
            IReadOnlyCollection<FieldDefinitionNode> fieldDefinitions)
        {
            foreach (FieldDefinitionNode fieldDefinition in fieldDefinitions)
            {
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

        private void DeclareFieldArguments(
            IObjectFieldDescriptor fieldDescriptor,
            FieldDefinitionNode fieldDefinition)
        {
            foreach (InputValueDefinitionNode inputFieldDefinition in
                fieldDefinition.Arguments)
            {
                fieldDescriptor.Argument(inputFieldDefinition.Name.Value,
                    a =>
                    {
                        IArgumentDescriptor descriptor = a.Description(inputFieldDefinition.Description?.Value)
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
