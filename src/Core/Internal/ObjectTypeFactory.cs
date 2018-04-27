using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    internal sealed class ObjectTypeFactory
        : ITypeFactory<ObjectTypeDefinitionNode, ObjectType>
    {
        public ObjectType Create(
            SchemaContext context,
            ObjectTypeDefinitionNode objectTypeDefinition)
        {
            ObjectTypeConfig config = new ObjectTypeConfig
            {
                SyntaxNode = objectTypeDefinition,
                Name = objectTypeDefinition.Name.Value,
                Description = objectTypeDefinition.Description?.Value,
            };

            ObjectType objectType = new ObjectType(config);
            config.Interfaces = () => GetInterfaces(
                context, objectTypeDefinition, objectType);
            config.Fields = () => GetFields(
                context, objectTypeDefinition, objectType);
            return objectType;
        }

        private IReadOnlyDictionary<string, InterfaceType> GetInterfaces(
            SchemaContext context,
            ObjectTypeDefinitionNode objectTypeDefinition,
            ObjectType objectType)
        {
            Dictionary<string, InterfaceType> interfaces =
                new Dictionary<string, InterfaceType>();

            foreach (NamedTypeNode type in objectTypeDefinition.Interfaces)
            {
                interfaces[type.Name.Value] = context
                    .GetOutputType<InterfaceType>(type.Name.Value);
            }

            return interfaces;
        }

        private Dictionary<string, Field> GetFields(
            SchemaContext context,
            ObjectTypeDefinitionNode objectTypeDefinition,
            ObjectType objectType)
        {
            Dictionary<string, Field> fields = new Dictionary<string, Field>(
                objectTypeDefinition.Fields.Count);

            foreach (FieldDefinitionNode fieldDefinition in
                objectTypeDefinition.Fields)
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
                    context, objectTypeDefinition, objectType,
                    fieldDefinition, field);
                config.Resolver = () => context.CreateResolver(
                    objectType.Name, field.Name);
                config.Type = () => context.GetOutputType(fieldDefinition.Type);
            }

            return fields;
        }

        private Dictionary<string, InputField> GetFieldArguments(
            SchemaContext context,
            ObjectTypeDefinitionNode objectTypeDefinition,
            ObjectType objectType,
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
                    Type = () => context.GetInputType(inputFieldDefinition.Type),
                    DefaultValue = () => inputFieldDefinition.DefaultValue
                };

                inputFields[config.Name] = new InputField(config);
            }

            return inputFields;
        }
    }
}
