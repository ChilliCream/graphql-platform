using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IFilterOperationField
        : IFilterField
    {
        int Operation { get; }
    }
}
