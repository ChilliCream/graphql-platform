using System.Buffers;

namespace CookieCrumble.Formatters;

/// <summary>
/// Formats a snapshot segment value for the snapshot file.
/// </summary>
public interface IMarkdownSnapshotValueFormatter
{
    /// <summary>
    /// Specifies if the formatter can handle the snapshot segment value.
    /// </summary>
    /// <param name="value">
    /// The snapshot segment value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the formatter can handle the snapshot segment value;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool CanHandle(object? value);

    /// <summary>
    /// Formats the specified snapshot segment value for the snapshot file.
    /// </summary>
    /// <param name="snapshot">
    /// The snapshot file writer.
    /// </param>
    /// <param name="value">
    ///  The snapshot segment vale.
    /// </param>
    void FormatMarkdown(IBufferWriter<byte> snapshot, object? value);
}
