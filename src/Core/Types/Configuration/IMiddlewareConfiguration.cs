using HotChocolate.Resolvers;

namespace HotChocolate.Configuration
{
    public interface IMiddlewareConfiguration
        : IFluent
    {
        IMiddlewareConfiguration Use(FieldMiddleware middleware);
    }
}
