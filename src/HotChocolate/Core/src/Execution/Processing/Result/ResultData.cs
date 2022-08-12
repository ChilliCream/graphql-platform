#nullable enable

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a result data object like an object or list.
/// </summary>
public abstract class ResultData
{
    /// <summary>
    /// Gets the parent result data object.
    /// </summary>
    internal virtual ResultData? Parent { get; set; }
}
