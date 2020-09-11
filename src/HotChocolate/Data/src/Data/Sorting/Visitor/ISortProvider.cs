using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Sorting
{
    public interface ISortProvider
    {
        IReadOnlyCollection<ISortFieldHandler> FieldHandlers { get; }

        IReadOnlyCollection<ISortOperationHandler> OperationHandlers { get; }

        FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);
    }
}

