namespace HotChocolate.Language
{
    /// <summary>
    /// Represents types that containe a name node.
    /// </summary>
    public interface IHasName
    {
        NameNode Name { get; }
    }
}
