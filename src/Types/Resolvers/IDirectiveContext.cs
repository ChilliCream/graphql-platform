using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public interface IDirectiveContext
        : IResolverContext
    {
        IDirective Directive { get; }

        object Result { get; set; }
    }
}
