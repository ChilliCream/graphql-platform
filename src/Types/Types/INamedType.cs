namespace HotChocolate.Types
{
    public interface INamedType
        : IType
        , INullableType
    {
        string Name { get; }

        string Description { get; }
    }
}
