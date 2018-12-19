using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal sealed class InputObjectTypeFactory
        : ITypeFactory<InputObjectTypeDefinitionNode, InputObjectType>
    {
        public InputObjectType Create(
            InputObjectTypeDefinitionNode node)
        {
            return new InputObjectType(d =>
            {
                d.SyntaxNode(node)
                    .Name(node.Name.Value)
                    .Description(node.Description?.Value);

                DeclareFields(d, node);
            });
        }

        private void DeclareFields(
            IInputObjectTypeDescriptor typeDescriptor,
            InputObjectTypeDefinitionNode node)
        {
            foreach (InputValueDefinitionNode inputField in node.Fields)
            {
                IInputFieldDescriptor descriptor = typeDescriptor
                    .Field(inputField.Name.Value)
                    .Description(inputField.Description?.Value)
                    .Type(inputField.Type)
                    .DefaultValue(inputField.DefaultValue)
                    .SyntaxNode(inputField);

                foreach (DirectiveNode directive in inputField.Directives)
                {
                    descriptor.Directive(directive);
                }
            }
        }
    }
}
