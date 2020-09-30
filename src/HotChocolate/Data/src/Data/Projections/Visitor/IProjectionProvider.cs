using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionProvider
    {
        FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);
    }
}
