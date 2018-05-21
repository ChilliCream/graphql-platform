namespace HotChocolate.Types
{
    public interface INamedType
        : IType
    {
        string Name { get; }
        string Description { get; }
    }
}
