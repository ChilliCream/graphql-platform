namespace HotChocolate.Fusion.Features;

internal sealed class SourceEnumValueMetadata
{
    /// <summary>
    /// Gets a value indicating whether the enum value or its declaring type is marked as
    /// inaccessible.
    /// </summary>
    public bool IsInaccessible { get; set; }
}
