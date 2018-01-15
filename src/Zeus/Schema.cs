using System;
using System.Collections.Generic;
using GraphQLParser.AST;
using Zeus.Types;

namespace Zeus
{
    public class Schema
        : ISchema
    {
        public static Schema Create(string schema, IResolverCollection resolvers)
        {
            throw new NotImplementedException();
        }
    }


    internal class SchemaSyntaxVisitor
        : SyntaxNodeWalker
    {
        private List<ObjectDeclaration> _typeDefinitions = new List<ObjectDeclaration>();
        private ObjectDeclaration _currentObject;
        private FieldDeclaration _currentField;

        public IReadOnlyCollection<ObjectDeclaration> ObjectTypes => _typeDefinitions;

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition objectTypeDefinition)
        {
            _typeDefinitions.Add(new ObjectDeclaration(
                objectTypeDefinition.Name.Value,
                DeserializeFieldDefinitions(objectTypeDefinition.Fields)));
        }

        private IEnumerable<FieldDeclaration> DeserializeFieldDefinitions(IEnumerable<GraphQLFieldDefinition> fieldDefinitions)
        {
            foreach (GraphQLFieldDefinition fieldDefinition in fieldDefinitions)
            {
                yield return new FieldDeclaration(fieldDefinition.Name.Value,
                    DeserializeArguments(fieldDefinition.Arguments));
            }
        }

        private IEnumerable<ArgumentDeclaration> DeserializeArguments(IEnumerable<GraphQLInputValueDefinition> inputValueDefinitions)
        {
            foreach (GraphQLInputValueDefinition inputValueDefinition in inputValueDefinitions)
            {
                yield return new ArgumentDeclaration(inputValueDefinition.Name.Value,
                    DeserializeType(inputValueDefinition.Type));
            }
        }

        private TypeDeclaration DeserializeType(GraphQLType type)
        {
            return null;
        }
    }

}

