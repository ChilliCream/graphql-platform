using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public class ProjectionProviderConfiguration : IHasScope
{
    public string? Scope { get; set; }

    public IList<Func<ProjectionProviderContext, IProjectionFieldHandler>> HandlerFactories { get; } = [];

    public IList<Func<ProjectionProviderContext, IProjectionFieldInterceptor>> InterceptorFactories { get; } =
        [];

    public IList<Func<ProjectionProviderContext, IProjectionOptimizer>> OptimizerFactories { get; } = [];
}
