using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchema
        : IEnumerable<IType> // TODO : maybe we should consider removing enumerable and having a concrete method GetAllTypes()
    {
        INamedType GetType(string typeName);
        T GetType<T>(string typeName) where T : INamedType;
    }
}
