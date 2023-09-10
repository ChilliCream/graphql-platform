using System.Collections.Generic;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a collection of filter fields and operations .
/// </summary>
public interface IFilterInfo
{
    /// <summary>
    /// Returns all filter fields of this value
    /// </summary>
    IReadOnlyList<IFilterFieldInfo> GetFields();

    /// <summary>
    /// Returns all filter operations of this value
    /// </summary>
    IReadOnlyList<IFilterOperationInfo> GetOperations();
}
