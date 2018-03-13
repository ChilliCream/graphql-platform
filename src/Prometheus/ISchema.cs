using System;
using Prometheus.Resolvers;
using Prometheus.Abstractions;

namespace Prometheus
{
    public interface ISchema
        : ISchemaDocument
    {
        IResolverCollection Resolvers { get; }

        IType InferType(ObjectTypeDefinition typeDefinition,
            FieldDefinition fieldDefinition, object obj);
    }
}

