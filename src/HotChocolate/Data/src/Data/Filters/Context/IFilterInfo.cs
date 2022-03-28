using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters;

public interface IFilterInfo : IFilterValue
{
    IValueNode ValueNode { get; }

    IReadOnlyList<IFilterFieldInfo> GetFields();

    IReadOnlyList<IFilterOperationInfo> GetOperations();

    IDictionary<string, object?> ToDictionary();
}
