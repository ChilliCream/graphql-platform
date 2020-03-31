namespace HotChocolate.Types
{
    public interface INamedType
        : INullableType
        , IHasName
        , IHasDescription
    {
        bool IsAssignableFrom(INamedType type);
    }
}
