using HotChocolate.Language;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a value of a filter.
/// </summary>
public interface IFilterValue
{
    /// <summary>
    /// Parses the <see cref="IValueNode" /> of this value into a .NET Type
    /// </summary>
    object? Value { get; }
}
