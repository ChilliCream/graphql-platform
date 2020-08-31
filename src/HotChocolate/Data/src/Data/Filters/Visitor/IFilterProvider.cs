using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProvider
    {
        IReadOnlyCollection<IFilterFieldHandler> FieldHandlers { get; }

        FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);
    }
}

