using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate
{
    public interface ISchema
    {
        ObjectType Query { get; }
        ObjectType Mutation { get; }
        ObjectType Subscription { get; }

        INamedType GetType(string typeName);
        T GetType<T>(string typeName) where T : INamedType;
        IReadOnlyCollection<INamedType> GetAllTypes();
    }
}
