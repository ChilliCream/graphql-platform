using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    internal class InterfaceTypeFactory
        : ITypeFactory<InterfaceTypeDefinitionNode, InterfaceType>
    {
        public InterfaceType Create(
            SchemaReaderContext context,
            InterfaceTypeDefinitionNode interfaceTypeDefinition)
        {
            InterfaceTypeConfig config = new InterfaceTypeConfig
            {
                SyntaxNode = interfaceTypeDefinition,
                Name = interfaceTypeDefinition.Name.Value,
                Description = interfaceTypeDefinition.Description?.Value,
            };

            InterfaceType interfaceType = new InterfaceType(config);
            config.Fields = () => GetFields(
                context, interfaceTypeDefinition, interfaceType);
            return interfaceType;
        }

        private Dictionary<string, Field> GetFields(
            SchemaReaderContext context,
            InterfaceTypeDefinitionNode interfaceTypeDefinition,
            InterfaceType interfaceType)
        {
            Dictionary<string, Field> fields = new Dictionary<string, Field>(
                interfaceTypeDefinition.Fields.Count);

            foreach (FieldDefinitionNode fieldDefinition in
                interfaceTypeDefinition.Fields)
            {
                FieldConfig config = new FieldConfig
                {
                    SyntaxNode = fieldDefinition,
                    Name = fieldDefinition.Name.Value,
                    Description = fieldDefinition.Description?.Value
                };

                Field field = new Field(config);
                fields[field.Name] = field;

                config.Arguments = () => GetFieldArguments(
                    context, interfaceTypeDefinition, interfaceType,
                    fieldDefinition, field);
                config.Type = () => context.GetOutputType(fieldDefinition.Type);
            }

            return fields;
        }

        private Dictionary<string, InputField> GetFieldArguments(
            SchemaReaderContext context,
            InterfaceTypeDefinitionNode interfaceTypeDefinition,
            InterfaceType interfaceType,
            FieldDefinitionNode fieldDefinition,
            Field field)
        {
            Dictionary<string, InputField> inputFields =
                new Dictionary<string, InputField>(
                    fieldDefinition.Arguments.Count);

            foreach (InputValueDefinitionNode inputFieldDefinition in
                fieldDefinition.Arguments)
            {
                InputFieldConfig config = new InputFieldConfig
                {
                    SyntaxNode = inputFieldDefinition,
                    Name = inputFieldDefinition.Name.Value,
                    Description = inputFieldDefinition.Description?.Value,
                    Type = () => context.GetInputType(inputFieldDefinition.Type)
                };

                InputField inputField = new InputField(config);
                inputFields[inputField.Name] = inputField;

                // TODO: default value
            }

            return inputFields;
        }
    }
}