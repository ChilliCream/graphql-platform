using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IAndField
        : IInputField
        , IHasRuntimeType
    {
        new IFilterInputType DeclaringType { get; }
    }
}
