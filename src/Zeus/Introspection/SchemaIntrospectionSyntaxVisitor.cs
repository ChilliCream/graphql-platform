using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;
using Zeus.Definitions;

namespace Zeus.Introspection
{
    internal class SchemaIntrospectionSyntaxVisitor
        : SyntaxNodeWalker
    {
        private readonly List<__Type> _types = new List<__Type>();

        protected override void VisitObjectTypeDefinition(GraphQLObjectTypeDefinition objectTypeDefinition)
        {
            IEnumerable<TypeDefinition> interfaces = objectTypeDefinition.Interfaces.Select(t => t.CreateTypeInfo());



            _types.Add(__Type.CreateObjectType(objectTypeDefinition.Name.Value, null, Array.Empty<__Field>(), interfaces));
        }

        /*
        protected override void VisitInputObjectTypeDefinition(GraphQLInputObjectTypeDefinition inputObjectTypeDefinition)
        {
            _types.Add(__Type.CreateInterfaceType())
        }

        private IEnumerable<__Field> GetFields(IEnumerable<GraphQLFieldDefinition> fieldDefinition)
        {

        }
         */
    }
}