using System;
using Zeus.Resolvers;
using Zeus.Abstractions;

namespace Zeus
{
    public interface ISchema
        : ISchemaDocument
    {        
        IResolverCollection Resolvers { get; }
    }
}

