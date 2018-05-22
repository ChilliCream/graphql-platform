using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Types.Factories
{
    internal abstract class ObjectTypeFactoryBase
    {
        protected IEnumerable<Field> GetFields(
            SchemaContext context,
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
                    Arguments = GetFieldArguments(context, fieldDefinition),
                    Type = () => context.GetOutputType(fieldDefinition.Type),
                    Resolver = () => context.CreateResolver(
                        typeName, fieldDefinition.Name.Value)
                });
            }

            return fields;
        }

        private IEnumerable<InputField> GetFieldArguments(
            SchemaContext context,
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
                    Type = () => context.GetInputType(inputFieldDefinition.Type),
                    DefaultValue = () => inputFieldDefinition.DefaultValue
                };

                inputFields[i++] = new InputField(config);
            }

            return inputFields;
        }
    }
}
