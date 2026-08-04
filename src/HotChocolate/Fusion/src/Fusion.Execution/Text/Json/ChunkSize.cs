namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Identifies the chunk-size bucket a document allocates its memory slabs at.
/// The ordinal maps to a byte size via <c>1 &lt;&lt; (10 + ordinal)</c>, so the
/// bucket fits the three size bits of a <c>Cursor</c>.
/// </summary>
internal enum ChunkSize
{
    /// <summary>1 KB chunks.</summary>
    Size1K,

    /// <summary>2 KB chunks.</summary>
    Size2K,

    /// <summary>4 KB chunks.</summary>
    Size4K,

    /// <summary>8 KB chunks.</summary>
    Size8K,

    /// <summary>16 KB chunks.</summary>
    Size16K,

    /// <summary>32 KB chunks.</summary>
    Size32K,

    /// <summary>64 KB chunks.</summary>
    Size64K,

    /// <summary>128 KB chunks.</summary>
    Size128K
}
