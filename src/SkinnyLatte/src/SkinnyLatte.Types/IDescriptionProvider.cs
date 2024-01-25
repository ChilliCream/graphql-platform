namespace SkinnyLatte.Types;

/// <summary>
/// GraphQL type system members that have a description.
/// </summary>
public interface IDescriptionProvider
{
    /// <summary>
    /// Gets the description of the type system member.
    /// </summary>
    string? Description { get; }
}
