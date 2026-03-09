using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public interface IAndField
    : IInputValueDefinition
    , IRuntimeTypeProvider
{
    IFilterInputType DeclaringType { get; }
}
