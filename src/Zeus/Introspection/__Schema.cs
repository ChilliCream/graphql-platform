using System.Collections.Generic;
using Zeus.Abstractions;

namespace Zeus.Introspection
{
    internal class __Schema
    {
        private readonly ISchema _schemas;

        public __Schema(ISchema schemas)
        {
            _schemas = schemas;
        }

        [GraphQLName("types")]
        public IEnumerable<__Type> GetTypes()
        {
            foreach (ITypeDefinition typeDefinition in _schemas)
            {
                __Type type = __Type.CreateType(typeDefinition);
                if (type != null)
                {
                    yield return type;
                }
            }
        }




    }
}