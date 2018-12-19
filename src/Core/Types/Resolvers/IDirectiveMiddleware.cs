namespace HotChocolate.Resolvers
{
    public interface IDirectiveMiddleware
    {
        string DirectiveName { get; }
    }
}
