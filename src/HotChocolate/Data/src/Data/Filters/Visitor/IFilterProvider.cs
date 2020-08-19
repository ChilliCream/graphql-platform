using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProvider
    {
        IReadOnlyCollection<IFilterFieldHandler> FieldHandlers { get; }

        IFilterExecutor<TEntityType> CreateExecutor<TEntityType>(NameString argumentName);
    }
}

