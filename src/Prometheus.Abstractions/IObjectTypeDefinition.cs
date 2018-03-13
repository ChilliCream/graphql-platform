using System.Collections.Generic;

namespace Prometheus.Abstractions
{
    public interface IObjectTypeDefinition
        : ITypeDefinition
    {
        IReadOnlyDictionary<string, FieldDefinition> Fields { get; }
    }
}