using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Introspection
{
    internal class __Schema
    {
        [GraphQLName("types")]
        public IEnumerable<__Type> GetTypes(Schema schema)
        {
            foreach (INamedType type in schema.GetAllTypes())
            {
                yield return __Type.CreateType(schema, type);
            }
        }

        [GraphQLName("queryType")]
        public __Type GetQueryType(Schema schema)
        {
            return __Type.CreateType(schema, schema.QueryType);
        }

        [GraphQLName("mutationType")]
        public __Type GetMutationType(Schema schema)
        {
            if (schema.MutationType == null)
            {
                return null;
            }

            return __Type.CreateType(schema, schema.MutationType);
        }

        [GraphQLName("subscriptionType")]
        public __Type GetSubscriptionType(Schema schema)
        {
            if (schema.SubscriptionType == null)
            {
                return null;
            }

            return __Type.CreateType(schema, schema.SubscriptionType);
        }

        [GraphQLName("directives")]
        public IEnumerable<__Directive> GetDirectives(Schema schema)
        {
            yield break;
        }
    }
}
