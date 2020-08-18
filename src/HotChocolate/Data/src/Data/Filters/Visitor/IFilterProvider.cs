using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProvider
    {
        IReadOnlyCollection<FilterFieldHandler> FilterFieldHandlers { get; }

        IFilterExecutor<TEntityType> CreateExecutor<TEntityType>(NameString argumentName);
    }


    public interface IFilterExecutor<TEntityType>
    {
        NameString ArgumentName { get; }

        FieldDelegate Execute { get; }
    }
}

