using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Types.Factories
{
    internal sealed class InterfaceTypeFactory
        : ITypeFactory<InterfaceTypeDefinitionNode, InterfaceType>
    {
        public InterfaceType Create(
            InterfaceTypeDefinitionNode node)
        {
            return new InterfaceType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                foreach (DirectiveNode directive in node.Directives)
                {
                    d.Directive(directive);
                }

                DeclareFields(d, node.Fields);
            });
        }

        private void DeclareFields(
            IInterfaceTypeDescriptor typeDescriptor,
            IReadOnlyCollection<FieldDefinitionNode> fieldDefinitions)
        {
            foreach (FieldDefinitionNode fieldDefinition in fieldDefinitions)
            {
                IInterfaceFieldDescriptor fieldDescriptor = typeDescriptor
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
            IInterfaceFieldDescriptor fieldDescriptor,
            FieldDefinitionNode fieldDefinition)
        {
            foreach (InputValueDefinitionNode inputFieldDefinition in
                fieldDefinition.Arguments)
            {
                fieldDescriptor.Argument(inputFieldDefinition.Name.Value,
                    a =>
                    {
                        foreach (DirectiveNode directive in
                            inputFieldDefinition.Directives)
                        {
                            fieldDescriptor.Directive(directive);
                        }

                        a.Description(inputFieldDefinition.Description?.Value)
                            .Type(inputFieldDefinition.Type)
                            .DefaultValue(inputFieldDefinition.DefaultValue)
                            .SyntaxNode(inputFieldDefinition);
                    });
            }
        }
    }
}
