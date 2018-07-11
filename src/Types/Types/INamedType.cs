namespace HotChocolate.Types
{
    public interface INamedType
        : INullableType
    {
        string Name { get; }

        string Description { get; }
    }
}
