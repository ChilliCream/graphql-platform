using HotChocolate.Language;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a value of a sorting.
/// </summary>
public interface ISortingValue
{
    /// <summary>
    /// Parses the <see cref="IValueNode" /> of this value into a .NET Type
    /// </summary>
    object? Value { get; }
}
