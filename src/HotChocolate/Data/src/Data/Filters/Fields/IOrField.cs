using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public interface IOrField
    : IInputValueDefinition
    , IHasRuntimeType
{
    IFilterInputType DeclaringType { get; }
}
