using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal class InputObjectTypeFactory
        : ITypeFactory<InputObjectTypeDefinitionNode, InputObjectType>
    {
        public InputObjectType Create(
            InputObjectTypeDefinitionNode node)
        {
            return new InputObjectType(new InputObjectTypeConfig
            {
                SyntaxNode = node,
                Name = node.Name.Value,
                Description = node.Description?.Value,
                Fields = CreateFields(node),
                NativeType = t =>
                {
                    if (t.TryGetTypeBinding(
                        node.Name.Value, out InputObjectTypeBinding binding))
                    {
                        return binding.Type;
                    }
                    return null;
                }
            });
        }

        private IEnumerable<InputField> CreateFields(
            InputObjectTypeDefinitionNode node)
        {
            foreach (InputValueDefinitionNode inputField in node.Fields)
            {
                yield return new InputField(new InputFieldConfig
                {
                    SyntaxNode = inputField,
                    Name = inputField.Name.Value,
                    Description = inputField.Description?.Value,
                    Type = t => t.GetInputType(inputField.Type),
                    DefaultValue = t => inputField.DefaultValue
                });
            }
        }
    }
}
