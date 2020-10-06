using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldHandler
    {
        bool CanHandle(ISelection selection);

        IProjectionFieldHandler Wrap(IProjectionFieldInterceptor interceptor);
    }
}
