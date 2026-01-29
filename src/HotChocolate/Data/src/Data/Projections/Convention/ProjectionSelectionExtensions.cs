using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections;

public static class ProjectionSelectionExtensions
{
    extension(Selection selection)
    {
        public IProjectionFieldHandler? ProjectionHandler { get => selection.Features.Get<IProjectionFieldHandler>(); }
    }
}
