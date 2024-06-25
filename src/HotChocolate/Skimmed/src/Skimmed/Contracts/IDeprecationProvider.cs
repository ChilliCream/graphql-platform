namespace HotChocolate.Skimmed;

public interface IDeprecationProvider : ITypeSystemMemberDefinition
{
    /// <summary>
    /// Defines if this <see cref="ITypeSystemMemberDefinition"/> is deprecated.
    /// </summary>
    bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason of this <see cref="ITypeSystemMemberDefinition"/>.
    /// </summary>
    string? DeprecationReason { get; }
}
