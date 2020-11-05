using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionSelection : ISelection
    {
        IProjectionFieldHandler Handler { get; }
    }
}
