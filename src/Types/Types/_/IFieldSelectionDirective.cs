using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IFieldSelectionDirective
    {
        void OnBeforeExecuteResolver(IResolverContext context);

        object OnAfterExecuteResolver(IResolverContext context, object resolverResult);
    }
}
