using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IOrField
        : IInputField
        , IHasRuntimeType
    {
        new IFilterInputType DeclaringType { get; }
    }
}
