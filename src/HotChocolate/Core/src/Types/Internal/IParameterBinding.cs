using HotChocolate.Resolvers;

namespace HotChocolate.Internal;

public interface IParameterBinding : IParameterHandler
{
    public T Execute<T>(IResolverContext context)
        => Execute<T>((IPureResolverContext)context);

    T Execute<T>(IPureResolverContext context);
}
