namespace SkinnyLatte.Types;

/// <summary>
/// GraphQL type system members that have directives.
/// </summary>
public interface IDirectiveProvider
{
    /// <summary>
    /// Gets the directives of this type system member.
    /// </summary>
    public object Directives { get; }
}
