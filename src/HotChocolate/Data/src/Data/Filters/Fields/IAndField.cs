using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public interface IAndField
    : IInputValueDefinition
    , IHasRuntimeType
{
    IFilterInputType DeclaringType { get; }
}
