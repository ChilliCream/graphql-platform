
namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents selections with inclusion conditions.
/// </summary>
public interface IOptionalSelection2
{
    /// <summary>
    /// Defines that this selection is only needed for internal processing.
    /// </summary>
    bool IsInternal { get; }

    /// <summary>
    /// Defines that this selection is conditional and will not always be included.
    /// </summary>
    bool IsConditional { get; }

    bool IsIncluded(long includeFlags, bool allowInternals = false);
}
