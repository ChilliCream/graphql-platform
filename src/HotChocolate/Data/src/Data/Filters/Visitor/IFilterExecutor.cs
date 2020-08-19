using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public interface IFilterExecutor<TEntityType>
    {
        NameString ArgumentName { get; }

        FieldDelegate Execute { get; }
    }
}

