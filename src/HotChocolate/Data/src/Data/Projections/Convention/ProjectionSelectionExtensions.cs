using HotChocolate.Execution.Processing;
using HotChocolate.Features;

namespace HotChocolate.Data.Projections;

public static class ProjectionSelectionExtensions
{
    extension(Selection selection)
    {
        public IProjectionFieldHandler? ProjectionHandler { get => selection.Features.Get<IProjectionFieldHandler>(); }
    }
}
