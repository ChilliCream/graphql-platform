using System;
using Zeus.Resolvers;
using Zeus.Abstractions;

namespace Zeus
{
    public interface ISchema
        : ISchemaDocument
    {
        IResolverCollection Resolvers { get; }

        IType InferType(ObjectTypeDefinition typeDefinition,
            FieldDefinition fieldDefinition, object obj);
    }
}

