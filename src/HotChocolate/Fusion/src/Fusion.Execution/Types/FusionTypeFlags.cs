namespace HotChocolate.Fusion.Types;

/// <summary>
/// Defines classification flags for Fusion type definitions.
/// </summary>
[Flags]
internal enum FusionTypeFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The type is shared across multiple source schemas.
    /// </summary>
    Shared = 1 << 0,

    /// <summary>
    /// The type is an entity — it is shared and has lookups
    /// that allow it to be resolved independently by source schemas.
    /// </summary>
    Entity = 1 << 1
}
