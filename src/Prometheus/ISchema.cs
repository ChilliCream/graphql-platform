using System;
using Prometheus.Resolvers;
using Prometheus.Abstractions;

namespace Prometheus
{
    public interface ISchema
        : ISchemaDocument
    {
        IResolverCollection Resolvers { get; }

        // TODO : move to execution
        IType ResolveAbstractType(ObjectTypeDefinition typeDefinition,
            FieldDefinition fieldDefinition, object result);
    }
}

