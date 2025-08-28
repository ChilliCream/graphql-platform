using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public class ProjectionProviderConfiguration : IHasScope
{
    public string? Scope { get; set; }

    public IList<(Type, IProjectionFieldHandler?)> Handlers { get; } = [];

    public IList<(Type, IProjectionFieldInterceptor?)> Interceptors { get; } = [];

    public IList<(Type, IProjectionOptimizer?)> Optimizers { get; } = [];
}
