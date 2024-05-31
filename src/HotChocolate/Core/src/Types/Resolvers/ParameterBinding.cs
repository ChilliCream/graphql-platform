namespace HotChocolate.Resolvers;

public abstract class ParameterBinding
{
    public abstract T Execute<T>(IResolverContext context);

    public abstract T Execute<T>(IPureResolverContext context);
}
