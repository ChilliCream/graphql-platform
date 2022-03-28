using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public class FilterOperationInfo : IFilterOperationInfo
{
    public FilterOperationInfo(
        IFilterValue value,
        IFilterMemberInfo? parent,
        IFilterOperationField field)
    {
        Parent = parent;
        Field = field;
        Value = value;
    }

    public IFilterMemberInfo? Parent { get; }

    public IFilterOperationField Field { get; }

    public IFilterValue? Value { get; }
}
