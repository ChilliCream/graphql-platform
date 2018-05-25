using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal class InputObjectTypeFactory
        : ITypeFactory<InputObjectTypeDefinitionNode, InputObjectType>
    {
        public InputObjectType Create(
            SchemaContext context, InputObjectTypeDefinitionNode node)
        {
            return new InputObjectType(new InputObjectTypeConfig
            {
                SyntaxNode = node,
                Name = node.Name.Value,
                Description = node.Description?.Value,
                Fields = CreateFields(context, node),
                NativeType = () =>
                {
                    if (context.TryGetNativeType(
                        node.Name.Value, out Type nativeType))
                    {
                        return nativeType;
                    }
                    return null;
                }
            });
        }

        private IEnumerable<InputField> CreateFields(
            SchemaContext context, InputObjectTypeDefinitionNode node)
        {
            foreach (InputValueDefinitionNode inputField in node.Fields)
            {
                yield return new InputField(new InputFieldConfig
                {
                    SyntaxNode = inputField,
                    Name = inputField.Name.Value,
                    Description = inputField.Description?.Value,
                    Type = () => context.GetInputType(inputField.Type),
                    DefaultValue = () => inputField.DefaultValue
                });
            }
        }
    }
}
