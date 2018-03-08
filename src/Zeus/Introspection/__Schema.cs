using System.Collections.Generic;
using Zeus.Abstractions;

namespace Zeus.Introspection
{
    internal class __Schema
    {
        [GraphQLName("queryType")]
        public __Type GetQueryType(ISchema schema)
        {
            return __Type.CreateType(schema.QueryType);
        }

        [GraphQLName("mutationType")]
        public __Type GetMutationType(ISchema schema)
        {
            if (schema.MutationType == null)
            {
                return null;
            }

            return __Type.CreateType(schema.MutationType);
        }


        [GraphQLName("types")]
        public IEnumerable<__Type> GetTypes(ISchema schema)
        {
            foreach (__Type scalarType in __Type.CreateScalarTypes())
            {
                yield return scalarType;
            }

            foreach (ITypeDefinition typeDefinition in schema)
            {
                yield return __Type.CreateType(typeDefinition);
            }
        }

        [GraphQLName("directives")]
        public IEnumerable<__Directive> GetDirectives()
        {
            yield break;
        }
    }
}