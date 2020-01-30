namespace HotChocolate.Types
{
    public interface IHasDirectives
    {
        IDirectiveCollection Directives { get; }
    }
}
