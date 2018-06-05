using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Types.Factories
{
    internal abstract class ObjectTypeFactoryBase
    {
        protected IEnumerable<Field> GetFields(
            string typeName,
            IReadOnlyCollection<FieldDefinitionNode> fieldDefinitions)
        {
            int i = 0;
            Field[] fields = new Field[fieldDefinitions.Count];

            foreach (FieldDefinitionNode fieldDefinition in fieldDefinitions)
            {
                fields[i++] = new Field(new FieldConfig
                {
                    SyntaxNode = fieldDefinition,
                    Name = fieldDefinition.Name.Value,
                    Description = fieldDefinition.Description?.Value,
                    Arguments = GetFieldArguments(fieldDefinition),
                    Type = t => t.GetOutputType(fieldDefinition.Type),
                    Resolver = r => r.GetResolver(
                        typeName, fieldDefinition.Name.Value)
                });
            }

            return fields;
        }

        private IEnumerable<InputField> GetFieldArguments(
            FieldDefinitionNode fieldDefinition)
        {
            int i = 0;
            InputField[] inputFields =
                new InputField[fieldDefinition.Arguments.Count];

            foreach (InputValueDefinitionNode inputFieldDefinition in
                fieldDefinition.Arguments)
            {
                InputFieldConfig config = new InputFieldConfig
                {
                    SyntaxNode = inputFieldDefinition,
                    Name = inputFieldDefinition.Name.Value,
                    Description = inputFieldDefinition.Description?.Value,
                    Type = t => t.GetInputType(inputFieldDefinition.Type),
                    DefaultValue = t => inputFieldDefinition.DefaultValue
                };

                inputFields[i++] = new InputField(config);
            }

            return inputFields;
        }
    }
}
