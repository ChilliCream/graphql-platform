using System;
using Zeus.Types;
using Zeus.Resolvers;

namespace Zeus
{
    public interface ISchema
    {
        ObjectDeclaration Query { get; }
        ObjectDeclaration Mutation { get; }
        IResolverCollection Resolvers { get; }

        bool TryGetObjectType(string typeName, out ObjectDeclaration objectType);
        bool TryGetInputType(string typeName, out InputDeclaration inputType);
    }
}

