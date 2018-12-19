namespace HotChocolate.Types
{
    public interface INamedType
        : INullableType
    {
        NameString Name { get; }

        string Description { get; }
    }
}
