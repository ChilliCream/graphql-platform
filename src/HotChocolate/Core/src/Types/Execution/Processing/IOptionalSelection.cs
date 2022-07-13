#nullable enable

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents selections with inclusion conditions.
/// </summary>
public interface IOptionalSelection
{
    /// <summary>
    /// Defines that this selection is only needed for internal processing.
    /// </summary>
    bool IsInternal { get; }

    /// <summary>
    /// Defines that this selection is conditional and will not always be included.
    /// </summary>
    bool IsConditional { get; }

    /// <summary>
    /// Defines if this selection will be included into the request execution.
    /// </summary>
    /// <param name="includeFlags">
    /// The execution include flags.
    /// </param>
    /// <param name="allowInternals">
    /// Allow internal selections to be included.
    /// </param>
    /// <returns>
    /// True, if this selection shall be included into the request execution.
    /// </returns>
    bool IsIncluded(long includeFlags, bool allowInternals = false);
}
