namespace HotChocolate.Language;

/// <summary>
/// Represents named syntax nodes.
/// </summary>
public interface IHasName
{
    /// <summary>
    /// Gets a name of the named syntax node.
    /// </summary>
    NameNode Name { get; }
}
