using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections;

public static class ProjectionSelectionExtensions
{
    extension(Selection selection)
    {
        public IProjectionFieldHandler? ProjectionHandler { get => selection.Features.Get<IProjectionFieldHandler>(); }

        /// <summary>
        /// Gets the dot-separated CLR member paths required from this selection's parent.
        /// </summary>
        public ReadOnlySpan<string> RequiredMemberPaths
            => selection.Features.Get<ProjectionRequirements>() is { } requirements
                ? requirements.MemberPaths
                : [];
    }
}
