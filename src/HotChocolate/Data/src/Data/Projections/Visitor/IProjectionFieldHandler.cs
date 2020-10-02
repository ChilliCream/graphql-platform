using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionFieldHandler
    {
        bool CanHandle(ISelection selection);

        Selection RewriteSelection(Selection selection);
    }
}
