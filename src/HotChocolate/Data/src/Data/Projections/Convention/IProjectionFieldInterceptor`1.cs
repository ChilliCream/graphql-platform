using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldInterceptor<in TContext>
        : IProjectionFieldInterceptor
        where TContext : IProjectionVisitorContext
    {
        void BeforeProjection(
            TContext context,
            ISelection selection);

        void AfterProjection(
            TContext context,
            ISelection selection);
    }
}
