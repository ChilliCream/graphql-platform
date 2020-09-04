namespace HotChocolate.Types
{
    public interface ITypeSystemObject
        : IHasName
        , IHasDescription
        , IHasReadOnlyContextData
        , IHasScope
        , ITypeSystemMember
    {
    }
}
