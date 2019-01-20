namespace HotChocolate.Resolvers
{
    public interface IDirectiveMiddleware
    {
        NameString DirectiveName { get; }
    }
}
