using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldInterceptor
    {
        bool CanHandle(ISelection field);
    }
}
