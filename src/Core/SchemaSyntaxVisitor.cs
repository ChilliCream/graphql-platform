using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    public class SchemaSyntaxVisitor
        : SyntaxNodeVisitor
    {
        private Dictionary<string, InterfaceTypeDefinitionNode> _interfaces = 
            new Dictionary<string, InterfaceTypeDefinitionNode>();
        private Dictionary<string, ObjectType> _objectTypes = 
            new Dictionary<string, ObjectType>();

        public IEnumerable<ObjectType> GetTypes()
        {
            return _objectTypes.Values;
        }

        protected override void VisitDocument(DocumentNode node)
        {
            VisitMany(node.Definitions);
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node)
        {
            ObjectTypeConfig config = new ObjectTypeConfig
            {
                SyntaxNode = node,
                Name = node.Name.Value,
                Description = node.Description?.Value,
            };

            config.Fields = () => GetFields(node);

            _objectTypes[config.Name] = new ObjectType(config);
        }

        private IReadOnlyCollection<Field> GetFields(
            ObjectTypeDefinitionNode node)
        {
            int index = 0;
            Field[] fields = new Field[node.Fields.Count];

            foreach (FieldDefinitionNode field in node.Fields)
            {
                FieldConfig config = new FieldConfig
                {
                    SyntaxNode = field,
                    Name = field.Name.Value,
                    Description = field.Description?.Value,
                    Arguments = () => GetFieldArguments(field)
                };

                fields[index++] = new Field(config);
            }

            return fields;
        }

        private IReadOnlyCollection<InputField> GetFieldArguments(
            FieldDefinitionNode field)
        {
            int index = 0;
            InputField[] inputFields = new InputField[field.Arguments.Count];

            foreach (InputValueDefinitionNode inputField in field.Arguments)
            {
                InputFieldConfig config = new InputFieldConfig
                {
                    SyntaxNode = inputField,
                    Name = inputField.Name.Value,
                    Description = inputField.Description?.Value,
                    Type = () => GetInputType(inputField.Type)
                };

                inputFields[index++] = new InputField(config);
            }

            return inputFields;
        }

        private IInputType GetInputType(ITypeNode type)
        {
            throw new NotImplementedException();
        }
    }
}