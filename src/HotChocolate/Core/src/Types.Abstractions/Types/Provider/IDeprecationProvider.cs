namespace HotChocolate.Types;

public interface IDeprecationProvider : ITypeSystemMember
{
    /// <summary>
    /// Defines if this <see cref="ITypeSystemMember"/> is deprecated.
    /// </summary>
    bool IsDeprecated { get; }

    /// <summary>
    /// Gets the deprecation reason of this <see cref="ITypeSystemMember"/>.
    /// </summary>
    string? DeprecationReason { get; }
}
