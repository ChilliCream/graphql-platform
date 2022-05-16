#nullable enable

namespace HotChocolate.Execution;

/// <summary>
/// Represents a result data object like an object or list.
/// </summary>
public interface IResultData
{
    /// <summary>
    /// Gets the parent result data object.
    /// </summary>
    IResultData? Parent { get; }
}
