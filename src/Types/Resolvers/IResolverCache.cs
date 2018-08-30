namespace HotChocolate.Resolvers
{
    public interface IResolverCache
    {
        bool TryAddResolver<T>(T resolver);
        bool TryGetResolver<T>(out T resolver);
    }

}
