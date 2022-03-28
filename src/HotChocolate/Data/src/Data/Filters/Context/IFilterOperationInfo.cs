using System.Collections;
using System.Collections.Generic;

namespace HotChocolate.Data.Filters;

public interface IFilterOperationInfo : IFilterMemberInfo
{
    IFilterOperationField Field { get; }
}

public interface IFilterValueCollection : IEnumerable<IFilterValue>, IFilterValue
{
}

public class FilterValueCollection : List<IFilterValue>, IFilterValueCollection
{
    public FilterValueCollection() : base()
    {
    }

    public FilterValueCollection(IEnumerable<IFilterValue> collection) : base(collection)
    {
    }

    public FilterValueCollection(int capacity) : base(capacity)
    {
    }
}

public interface IFilterValue
{
}
