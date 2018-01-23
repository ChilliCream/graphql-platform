using System;
using Zeus.Definitions;
using Zeus.Resolvers;

namespace Zeus
{
    public interface ISchema
    {
        ObjectTypeDefinition Query { get; }
        ObjectTypeDefinition Mutation { get; }
        IResolverCollection Resolvers { get; }

        bool TryGetObjectType(string typeName, out ObjectTypeDefinition objectType);
        bool TryGetInputType(string typeName, out InputObjectTypeDefinition inputType);
    }
}

