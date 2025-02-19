namespace HotChocolate.Types;

/// <summary>
/// A type system member that has a description.
/// </summary>
public interface IDescriptionProvider : ITypeSystemMember
{
    /// <summary>
    /// Gets the description of the <see cref="ITypeSystemMember"/>.
    /// </summary>
    string? Description { get; }
}
