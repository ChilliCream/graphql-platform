namespace HotChocolate.Features;

/// <summary>
/// Represents a sealable feature.
/// </summary>
public interface ISealable
{
    /// <summary>
    /// Defined if this feature is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Seals this feature.
    /// </summary>
    void Seal();
}
