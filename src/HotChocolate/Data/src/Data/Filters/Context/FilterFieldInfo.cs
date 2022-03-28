using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

public class FilterFieldInfo : IFilterFieldInfo
{
    public FilterFieldInfo(
        IFilterValue value,
        IFilterMemberInfo? parent,
        IFilterField field)
    {
        Parent = parent;
        Field = field;
        Value = value;
    }

    public IFilterMemberInfo? Parent { get; }

    public IFilterField Field { get; }

    public IFilterValue? Value { get; }
}
