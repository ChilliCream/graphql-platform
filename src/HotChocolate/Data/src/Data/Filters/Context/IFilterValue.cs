using System.Collections.Generic;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a value of a filter.
/// </summary>
public interface IFilterValue
{
    /// <summary>
    /// Parses the <see cref="IValueNode" /> of this value into a .NET Type
    /// </summary>
    object? ParseValue();

    /// <summary>
    /// Returns all filter fields of this value
    /// </summary>
    IReadOnlyList<IFilterFieldInfo> GetFields();

    /// <summary>
    /// Returns all filter operations of this value
    /// </summary>
    IReadOnlyList<IFilterOperationInfo> GetOperations();
}
