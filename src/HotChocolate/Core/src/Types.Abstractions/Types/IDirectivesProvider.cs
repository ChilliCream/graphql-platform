namespace HotChocolate.Types;

/// <summary>
/// A type system member that has directives.
/// </summary>
public interface IDirectivesProvider : ITypeSystemMemberDefinition
{
    /// <summary>
    /// Gets the directives of the type system member.
    /// </summary>
    /// <value>
    /// The directives of the type system member.
    /// </value>
    IReadOnlyDirectiveCollection Directives { get; }
}
