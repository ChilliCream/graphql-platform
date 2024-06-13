namespace HotChocolate.Skimmed;

/// <summary>
/// A type system member that has a description.
/// </summary>
public interface IDescriptionProvider : ITypeSystemMemberDefinition
{
    /// <summary>
    /// Gets the description of the <see cref="ITypeSystemMemberDefinition"/>.
    /// </summary>
    string? Description { get; }
}
