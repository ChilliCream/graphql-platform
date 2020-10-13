using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections
{
    public class ProjectionSelection
        : Selection
        , IProjectionSelection
    {
        public ProjectionSelection(
            IProjectionFieldHandler handler,
            Selection selection)
            : base(selection)
        {
            Handler = handler;
        }

        public IProjectionFieldHandler Handler { get; }

        public static ProjectionSelection From(
            Selection selection,
            IProjectionFieldHandler handler) =>
            new ProjectionSelection(handler, selection);
    }
}
