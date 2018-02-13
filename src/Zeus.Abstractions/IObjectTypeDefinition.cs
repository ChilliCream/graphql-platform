using System.Collections.Generic;

namespace Zeus.Abstractions
{
    public interface IObjectTypeDefinition
        : ITypeDefinition
    {
        IReadOnlyDictionary<string, FieldDefinition> Fields { get; }
    }
}