namespace HotChocolate.Skimmed;

public interface ICanBeDeprecated : ITypeSystemMember
{
    /// <summary>
    /// Defines if this <see cref="ITypeSystemMember"/> is deprecated.
    /// </summary>
    bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the deprecation reason of this <see cref="ITypeSystemMember"/>.
    /// </summary>
    string? DeprecationReason { get; set; }
}